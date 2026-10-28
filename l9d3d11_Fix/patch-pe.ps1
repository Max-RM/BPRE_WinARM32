# patch-pe.ps1
#
# Minimal PE32 reader/patcher used by the Bad Piggies ARM32 L9D3D11 fix.
# It does exactly what tools/badpiggies/make_badpiggies_package.py does with its
# `PE` class: read the import table, append a new DLL name to the slack of the
# section that holds it, and repoint IMAGE_IMPORT_DESCRIPTOR.Name.
#
# Only PE32 (0x10B) is supported. The ARM32 UWP player is PE32.
# Dot-source this file; it only defines functions.

function Get-PeInfo {
    # Deliberately untyped: PowerShell copies (element by element) a value that is
    # coerced to [byte[]], which costs seconds on a 47 MB UnityPlayer.dll. The
    # contents are validated below instead.
    param([Parameter(Mandatory = $true)]$Bytes)

    if (-not ($Bytes -is [byte[]])) { throw 'PE: expected a byte array' }

    if ($Bytes.Length -lt 0x40) { throw 'PE: file is too small' }
    $e = [BitConverter]::ToUInt32($Bytes, 0x3C)
    if ($e -lt 0x40 -or $e + 24 -gt $Bytes.Length) { throw 'PE: bad e_lfanew' }
    if ([System.Text.Encoding]::ASCII.GetString($Bytes, [int]$e, 4) -ne "PE`0`0") {
        throw 'PE: MZ/PE signature not found'
    }

    $machine  = [BitConverter]::ToUInt16($Bytes, [int]$e + 4)
    $numSec   = [BitConverter]::ToUInt16($Bytes, [int]$e + 6)
    $optSize  = [BitConverter]::ToUInt16($Bytes, [int]$e + 20)
    $opt      = [int]$e + 24
    if ([BitConverter]::ToUInt16($Bytes, $opt) -ne 0x10B) {
        throw 'PE: not PE32 (only ARM32/x86 images are supported)'
    }

    $secTable = $opt + $optSize
    $sections = New-Object System.Collections.Generic.List[object]
    for ($i = 0; $i -lt $numSec; $i++) {
        $o = $secTable + 40 * $i
        if ($o + 40 -gt $Bytes.Length) { throw 'PE: truncated section table' }
        $name = [System.Text.Encoding]::ASCII.GetString($Bytes, $o, 8).TrimEnd([char]0)
        $sections.Add([pscustomobject]@{
            Name             = $name
            VirtualSize      = [BitConverter]::ToUInt32($Bytes, $o + 8)
            VirtualAddress   = [BitConverter]::ToUInt32($Bytes, $o + 12)
            SizeOfRawData    = [BitConverter]::ToUInt32($Bytes, $o + 16)
            PointerToRawData = [BitConverter]::ToUInt32($Bytes, $o + 20)
            HeaderOffset     = $o
        })
    }

    return [pscustomobject]@{
        Bytes            = $Bytes
        Machine          = $machine
        OptionalHeader   = $opt
        SectionAlignment = [BitConverter]::ToUInt32($Bytes, $opt + 32)
        ImportRva        = [BitConverter]::ToUInt32($Bytes, $opt + 96 + 8)
        ImportSize       = [BitConverter]::ToUInt32($Bytes, $opt + 96 + 12)
        ExportRva        = [BitConverter]::ToUInt32($Bytes, $opt + 96 + 0)
        ExportSize       = [BitConverter]::ToUInt32($Bytes, $opt + 96 + 4)
        Sections         = $sections
    }
}

function Convert-RvaToOffset {
    param(
        [Parameter(Mandatory = $true)]$Info,
        [Parameter(Mandatory = $true)][uint32]$Rva
    )

    foreach ($s in $Info.Sections) {
        $size = [Math]::Max([uint32]$s.VirtualSize, [uint32]$s.SizeOfRawData)
        if ($Rva -ge $s.VirtualAddress -and $Rva -lt ([uint32]$s.VirtualAddress + $size)) {
            return [int]([uint32]$s.PointerToRawData + ($Rva - [uint32]$s.VirtualAddress))
        }
    }
    throw ("PE: RVA 0x{0:X8} is in no section" -f $Rva)
}

function Read-PeCString {
    # Untyped $Bytes on purpose - see Get-PeInfo. [Array]::IndexOf scans natively.
    param(
        [Parameter(Mandatory = $true)]$Bytes,
        [Parameter(Mandatory = $true)][int]$Offset
    )

    if ($Offset -lt 0 -or $Offset -ge $Bytes.Length) { throw 'PE: string offset out of range' }
    $end = [System.Array]::IndexOf($Bytes, [byte]0, $Offset)
    if ($end -lt 0) { throw 'PE: unterminated string' }
    return [System.Text.Encoding]::ASCII.GetString($Bytes, $Offset, $end - $Offset)
}

function Get-PeImportDescriptors {
    param([Parameter(Mandatory = $true)]$Info)

    if (-not $Info.ImportRva) { throw 'PE: no import directory' }
    $bytes = $Info.Bytes
    $off = Convert-RvaToOffset $Info $Info.ImportRva
    $list = New-Object System.Collections.Generic.List[object]
    while ($true) {
        if ($off + 20 -gt $bytes.Length) { throw 'PE: truncated import directory' }
        $oft   = [BitConverter]::ToUInt32($bytes, $off)
        $tds   = [BitConverter]::ToUInt32($bytes, $off + 4)
        $fwd   = [BitConverter]::ToUInt32($bytes, $off + 8)
        $name  = [BitConverter]::ToUInt32($bytes, $off + 12)
        $ft    = [BitConverter]::ToUInt32($bytes, $off + 16)
        if ($name -eq 0 -and $ft -eq 0) { break }
        $nameOff = Convert-RvaToOffset $Info $name
        $list.Add([pscustomobject]@{
            Offset        = $off
            Name          = (Read-PeCString -Bytes $bytes -Offset $nameOff)
            NameRva       = $name
            TimeDateStamp = $tds
            LookupRva     = if ($oft) { $oft } else { $ft }
        })
        $off += 20
    }
    return $list
}

function Get-PeImports {
    param([Parameter(Mandatory = $true)]$Info)
    return @(Get-PeImportDescriptors -Info $Info | ForEach-Object { $_.Name })
}

function Get-PeImportedFunctions {
    # Names imported from one DLL (ordinal imports are returned as "#<n>").
    # Used to prove that every d3d11 entry point UnityPlayer.dll needs is exported
    # by the shim - a mismatch would only show up as a launch failure on the device.
    param(
        [Parameter(Mandatory = $true)]$Info,
        [Parameter(Mandatory = $true)][string]$DllName
    )

    $hits = @(Get-PeImportDescriptors -Info $Info | Where-Object { $_.Name -ieq $DllName })
    if ($hits.Count -ne 1) { throw ("PE: expected one import descriptor for '{0}', found {1}" -f $DllName, $hits.Count) }

    $bytes = $Info.Bytes
    $off = Convert-RvaToOffset $Info ([uint32]$hits[0].LookupRva)
    # 0x80000000 does not fit an Int32 literal in PowerShell, so build the flag explicitly.
    $ordinalFlag = [uint32]2147483648
    $names = New-Object System.Collections.Generic.List[string]
    while ($true) {
        if ($off + 4 -gt $bytes.Length) { throw "PE: truncated import lookup table for $DllName" }
        $entry = [BitConverter]::ToUInt32($bytes, $off)
        if ($entry -eq 0) { break }
        if (($entry -band $ordinalFlag) -ne 0) {
            $names.Add('#' + ($entry -band 0x7FFFFFFF))
        } else {
            # IMAGE_IMPORT_BY_NAME: WORD hint, then the name.
            $names.Add((Read-PeCString -Bytes $bytes -Offset ((Convert-RvaToOffset $Info ([uint32]$entry)) + 2)))
        }
        $off += 4
    }
    return $names
}

function Get-PeExports {
    param([Parameter(Mandatory = $true)]$Info)

    if (-not $Info.ExportRva) { return @() }
    $bytes = $Info.Bytes
    $dir = Convert-RvaToOffset $Info $Info.ExportRva
    $numNames = [BitConverter]::ToUInt32($bytes, $dir + 24)
    $namesRva = [BitConverter]::ToUInt32($bytes, $dir + 32)
    if ($numNames -eq 0 -or $namesRva -eq 0) { return @() }
    $namesOff = Convert-RvaToOffset $Info $namesRva
    $names = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $numNames; $i++) {
        $rva = [BitConverter]::ToUInt32($bytes, $namesOff + 4 * $i)
        $names.Add((Read-PeCString -Bytes $bytes -Offset (Convert-RvaToOffset $Info $rva)))
    }
    return $names
}

function Add-PeString {
    param(
        [Parameter(Mandatory = $true)]$Info,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $bytes = $Info.Bytes
    $sec = $null
    foreach ($s in $Info.Sections) {
        $size = [Math]::Max([uint32]$s.VirtualSize, [uint32]$s.SizeOfRawData)
        if ($Info.ImportRva -ge $s.VirtualAddress -and $Info.ImportRva -lt ([uint32]$s.VirtualAddress + $size)) {
            $sec = $s
            break
        }
    }
    if (-not $sec) { throw 'PE: the import table is in no section' }

    # Same layout rule as make_badpiggies_package.py: append at the even-aligned end of VirtualSize,
    # inside SizeOfRawData and inside the page-aligned footprint, and only into zero bytes.
    $start = [uint32]$sec.VirtualSize
    if ($start % 2 -ne 0) { $start = $start + 1 }
    $need = [uint32]$start + [uint32]([Text.Encoding]::ASCII.GetByteCount($Text) + 1)
    if ($need -gt [uint32]$sec.SizeOfRawData) {
        throw ("PE: no slack in section {0} for '{1}' (VirtualSize={2}, SizeOfRawData={3})" -f $sec.Name, $Text, $sec.VirtualSize, $sec.SizeOfRawData)
    }
    $align = [uint32]$Info.SectionAlignment
    $rem = [uint32]$sec.VirtualSize % $align
    $footprint = if ($rem -eq 0) { [uint32]$sec.VirtualSize } else { [uint32]$sec.VirtualSize + ($align - $rem) }
    if ($need -gt $footprint) {
        throw ("PE: writing '{0}' would grow section {1} into the next page" -f $Text, $sec.Name)
    }

    $off = [int]([uint32]$sec.PointerToRawData + $start)
    $len = [Text.Encoding]::ASCII.GetByteCount($Text)
    for ($i = 0; $i -lt $len + 1; $i++) {
        if ($off + $i -ge $bytes.Length) { throw 'PE: string write past end of file' }
        if ($bytes[$off + $i] -ne 0) {
            throw ("PE: slack of section {0} at 0x{1:X} is not zero" -f $sec.Name, $off + $i)
        }
    }

    $enc = [Text.Encoding]::ASCII.GetBytes($Text)
    [Array]::Copy($enc, 0, $bytes, $off, $enc.Length)
    $bytes[$off + $enc.Length] = 0
    [BitConverter]::GetBytes([uint32]$need).CopyTo($bytes, [int]$sec.HeaderOffset + 8)
    $sec.VirtualSize = $need

    return [uint32]([uint32]$sec.VirtualAddress + $start)
}

function Set-PeImportName {
    param(
        [Parameter(Mandatory = $true)]$Info,
        [Parameter(Mandatory = $true)][string]$Old,
        [Parameter(Mandatory = $true)][string]$New
    )

    $hits = @(Get-PeImportDescriptors -Info $Info | Where-Object { $_.Name -ieq $Old })
    if ($hits.Count -ne 1) {
        $present = (Get-PeImports -Info $Info) -join ', '
        throw ("PE: expected exactly one import descriptor for '{0}', found {1} (imports: {2})" -f $Old, $hits.Count, $present)
    }

    $newRva = Add-PeString -Info $Info -Text $New
    [BitConverter]::GetBytes([uint32]$newRva).CopyTo($Info.Bytes, $hits[0].Offset + 12)
    [BitConverter]::GetBytes([uint32]0).CopyTo($Info.Bytes, $hits[0].Offset + 4)

    return [pscustomobject]@{ Old = $Old; New = $New; NameRva = [uint32]$newRva }
}

function Update-PeImportFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][hashtable]$Renames
    )

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $info = Get-PeInfo -Bytes $bytes
    $changed = New-Object System.Collections.Generic.List[object]

    foreach ($old in $Renames.Keys) {
        $changed.Add((Set-PeImportName -Info $info -Old $old -New $Renames[$old]))
    }

    [System.IO.File]::WriteAllBytes($Path, $bytes)

    # Re-read and prove the result instead of trusting the in-memory edit.
    $check = Get-PeInfo -Bytes ([System.IO.File]::ReadAllBytes($Path))
    $now = Get-PeImports -Info $check
    foreach ($old in $Renames.Keys) {
        if ($now | Where-Object { $_ -ieq $old }) {
            throw ("PE: {0} still imports {1} after patching" -f (Split-Path -Leaf $Path), $old)
        }
        $new = $Renames[$old]
        if (-not ($now | Where-Object { $_ -ieq $new })) {
            throw ("PE: {0} does not import {1} after patching" -f (Split-Path -Leaf $Path), $new)
        }
    }

    return $changed
}
