# Build-UWP-ARM32-L9D3D11-RT-Fix.ps1
#
# Builds the alternative UWP ARM32 package that renders Bad Piggies::Rebooted on
# a Direct3D feature-level 9_1 GPU (Surface RT, Surface 2 - Windows 10 ARM32
# build 15035) by putting the l9d3d11 shim between Unity and Direct3D.
#
# Unlike the Win32Craft build (a loose folder registered with Add-AppxPackage
# -Register) this keeps the UWP package intact: the source .appx is unpacked,
# three payload files get their imports repointed, the shim, the shader table and
# the app-local VC++ runtime are added, the VCLibs.140 framework dependency is
# removed from AppxManifest.xml, and the package is packed and signed again with
# the same certificate as every other BPRE package.
#
# Everything the package needs is taken from l9d3d11_Fix\ (sources, shader table,
# VC++ runtime, the bundled rt2.1 compiler and a prebuilt l9d3d11.dll), so this
# script is the only entry point - see l9d3d11_Fix\README.md for the details.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\Build-UWP-ARM32-L9D3D11-RT-Fix.ps1
#   ...-RT-Fix.ps1 -ForceShimRebuild          # rebuild l9d3d11.dll from src\ first
#   ...-RT-Fix.ps1 -AppxPath "D:\...\Other_ARM.appx"
#   ...-RT-Fix.ps1 -NoPublish                 # build/sign, keep it in %TEMP%
#
# Output: Publish\BPRE_UWP-ARM32-L9D3D11\  (appx + cer + installer + README)

[CmdletBinding()]
param(
    # Source package. Default: the newest Publish\BPRE_UWP-ARM32\*_ARM32.appx
    # (folders published before the rename still contain *_ARM.appx).
    [string]$AppxPath,
    # Output folder. Default: Publish\BPRE_UWP-ARM32-L9D3D11
    [string]$Destination,
    # Passed to Build-UWP-ARM32.ps1 when the source package has to be built.
    [string]$UnityPath,
    [string]$MSBuildPath,
    # Rebuild l9d3d11.dll from l9d3d11_Fix\src before packaging.
    [switch]$ForceShimRebuild,
    # Pack a game build whose UnityPlayer.dll differs from the tested one. The
    # shim's capability patches are tied to RVAs of the tested DLL, so this is a
    # diagnostic option, not a supported build.
    [switch]$AllowDifferentUnityPlayer,
    # Keep %TEMP%\bpre-l9d3d11 (unpacked stage, tool logs) after a failure.
    [switch]$KeepStage,
    # Build and sign, but keep the package in %TEMP% instead of Publish\.
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

$FixRoot        = Join-Path $ProjectRoot 'l9d3d11_Fix'
. (Join-Path $FixRoot 'patch-pe.ps1')

$ShimPath       = Join-Path $FixRoot 'l9d3d11.dll'
$TablePath      = Join-Path $FixRoot 'data\l9_replacements.bin'
$VcLibsFolder   = Join-Path $FixRoot 'vclibs'
$ShimBuild      = Join-Path $FixRoot 'build-l9d3d11.ps1'
# Scripts that are packed into the appx so the package can also be installed from
# an unpacked folder (no certificate), plus a shortcut to back up the saves.
$LooseDir       = Join-Path $FixRoot 'loose'
$LooseFiles     = @('Install-Loose.bat', 'Install-Loose.ps1', 'Open-Saves.bat', 'README-LOOSE.txt')

$ShimName       = 'l9d3d11.dll'
$TableRelPath   = 'Data\bp\l9_replacements.bin'
$SourceAppx     = $AppxPath
if (-not $Destination) { $Destination = Join-Path $PublishRoot 'BPRE_UWP-ARM32-L9D3D11' }

$TestedUnityPlayerSha = 'E3530FD5D6CC0E431E5EDB69623B7C18E1A57A8013914D958C5F02711CB03793'
$TableMagic           = 'L9SW'

$VcLibs = @(
    'vcruntime140.dll', 'msvcp140.dll', 'msvcp140_1.dll', 'msvcp140_2.dll',
    'msvcp140_atomic_wait.dll', 'msvcp140_codecvt_ids.dll', 'vccorlib140.dll', 'concrt140.dll'
)

# Import repointing, exactly as tools/badpiggies/make_badpiggies_package.py does it:
# the *_APP framework imports become the app-local plain names, and UnityPlayer's
# d3d11.dll becomes the shim (which forwards everything to the real d3d11.dll).
# The game exe is named after the Unity product ("BadPiggiesRebooted.exe" in the
# tested package, "Bad PiggiesRebooted.exe" when rebuilt from the project), so its
# name is read from AppxManifest.xml after unpacking instead of being hard-coded.
$GameExeRenames = [ordered]@{
    'VCRUNTIME140_APP.dll' = 'vcruntime140.dll'
    'vccorlib140_app.DLL'  = 'vccorlib140.dll'
}
$BinaryRenames = [ordered]@{
    'UnityPlayer.dll' = [ordered]@{
        'VCRUNTIME140_APP.dll' = 'vcruntime140.dll'
        'MSVCP140_APP.dll'     = 'msvcp140.dll'
        'vccorlib140_app.DLL'  = 'vccorlib140.dll'
        'd3d11.dll'            = $ShimName
    }
    'GameAssembly.dll' = [ordered]@{
        'VCRUNTIME140_APP.dll' = 'vcruntime140.dll'
        'MSVCP140_APP.dll'     = 'msvcp140.dll'
    }
}

$RequiredShimExports = @(
    'D3D11CreateDevice',
    'CreateDirect3D11SurfaceFromDXGISurface',
    'CreateDirect3D11DeviceFromDXGIDevice'
)

$StageRoot = Join-Path $env:TEMP 'bpre-l9d3d11'
$Stage     = Join-Path $StageRoot 'stage'
$Report    = New-Object System.Collections.Generic.List[string]
$succeeded = $false

# ---------------------------------------------------------------------------
# helpers
# ---------------------------------------------------------------------------

function Add-Report {
    param([string]$Text = '')
    $Report.Add($Text)
}

function Get-SdkTool {
    # makeappx.exe / signtool.exe from the SDK the project builds against, or from
    # the newest installed one when that version is not present.
    param([Parameter(Mandatory = $true)][string]$Name)

    $roots = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'),
        (Join-Path $env:ProgramFiles 'Windows Kits\10\bin')
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    foreach ($root in $roots) {
        $versions = @(Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending | ForEach-Object { $_.Name })
        $versions = @($WinSdkVersion) + @($versions | Where-Object { $_ -ne $WinSdkVersion })
        foreach ($version in $versions) {
            $candidate = Join-Path (Join-Path $root $version) "x64\$Name.exe"
            if (Test-Path -LiteralPath $candidate) { return $candidate }
        }
    }
    throw "$Name.exe not found under Windows Kits\10\bin. Install the Windows 10 SDK ($WinSdkVersion or newer)."
}

function Invoke-Native {
    # Runs a native tool, keeps stdout+stderr, and turns a non-zero exit code into
    # one readable exception (PowerShell would otherwise stop on the first stderr
    # line because $ErrorActionPreference is 'Stop').
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$What
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $FilePath @Arguments 2>&1
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
    }

    if ($code -ne 0) {
        $text = (@($output) | ForEach-Object { "$_" }) -join "`n"
        throw ("{0} failed with exit code {1}.`n{2}" -f $What, $code, $text.Trim())
    }
    return @($output)
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Invoke-ShimBuild {
    param([switch]$Force)

    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $ShimBuild)
    if ($Force) { $arguments += '-Force' }

    if ($Force) {
        Write-Step 'Rebuilding l9d3d11.dll'
    } else {
        Write-Step 'Building l9d3d11.dll (bundled rt2.1 compiler)'
    }
    $output = Invoke-Native -FilePath 'powershell.exe' -Arguments $arguments -What 'l9d3d11.dll build'
    foreach ($line in $output) { if ("$line".Trim()) { Write-Host "  $line" } }
    Assert-Path -Path $ShimPath -Label 'l9d3d11.dll'
    Write-Ok "Shim rebuilt: $ShimPath"
}

function Assert-Shim {
    param([Parameter(Mandatory = $true)][string]$Path)

    $info = Get-PeInfo -Bytes ([System.IO.File]::ReadAllBytes($Path))
    if ($info.Machine -ne 0x1C4) { throw ("{0} is not an ARM32 image (machine 0x{1:X4})" -f (Split-Path -Leaf $Path), $info.Machine) }

    $imports = @(Get-PeImports -Info $info)
    $foreign = @($imports | Where-Object { $_ -ine 'KERNEL32.dll' })
    if ($foreign.Count -gt 0) {
        throw ("{0} imports more than KERNEL32.dll: {1} (it must not need the VC++ runtime)" -f (Split-Path -Leaf $Path), ($foreign -join ', '))
    }

    $exports = @(Get-PeExports -Info $info)
    $missing = @($RequiredShimExports | Where-Object { $name = $_; -not ($exports | Where-Object { $_ -ceq $name }) })
    if ($missing.Count -gt 0) {
        throw ("{0} does not export: {1}" -f (Split-Path -Leaf $Path), ($missing -join ', '))
    }

    return [pscustomobject]@{ Exports = $exports; Size = (Get-Item -LiteralPath $Path).Length }
}

function Assert-ShaderTable {
    param([Parameter(Mandatory = $true)][string]$Path)

    Assert-Path -Path $Path -Label 'shader replacement table'
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 12) { throw "shader table is too small: $Path" }
    $magic = [System.Text.Encoding]::ASCII.GetString($bytes, 0, 4)
    if ($magic -ne $TableMagic) {
        throw ("{0} is not a {1} table (magic is '{2}')" -f (Split-Path -Leaf $Path), $TableMagic, $magic)
    }
    $version = [BitConverter]::ToUInt32($bytes, 4)
    $count   = [BitConverter]::ToUInt32($bytes, 8)
    if ($version -ne 1) { throw ("unsupported shader table version {0}" -f $version) }
    if ($count -eq 0) { throw 'the shader table is empty - the shim would fall back to no replacements' }
    return [pscustomobject]@{ Version = $version; Count = $count; Size = $bytes.Length }
}

function Assert-LooseFiles {
    # These files are user-facing and end up inside the signed package, so they are
    # checked here: plain ASCII, CRLF, and an installer that stays inside the
    # Constrained Language Mode subset that build 15035 enforces.
    param([Parameter(Mandatory = $true)][string]$Folder)

    Assert-Path -Path $Folder -Label 'unpacked-install scripts folder'

    $texts = @{}
    foreach ($name in $LooseFiles) {
        $path = Join-Path $Folder $name
        Assert-Path -Path $path -Label "unpacked-install file $name"

        $bytes = [System.IO.File]::ReadAllBytes($path)
        if ($bytes.Length -eq 0) { throw "$name is empty" }
        if (@($bytes | Where-Object { $_ -gt 0x7F }).Count -gt 0) {
            throw "$name is not plain ASCII - it has to run on Windows 10 ARM32 build 15035"
        }

        $text = [System.Text.Encoding]::ASCII.GetString($bytes)
        if ($text -notmatch "`r`n") { throw "$name does not use CRLF line endings" }
        $texts[$name] = $text
    }

    # CLM blocks Add-Type, New-Object and .NET method calls. Comments are ignored.
    $ps1 = $texts['Install-Loose.ps1']
    $code = @($ps1 -split "`r`n" | Where-Object { $_.Trim() -notlike '#*' }) -join "`n"
    foreach ($banned in @('Add-Type', 'New-Object', '[System.', '[IO.', ']::')) {
        if ($code.IndexOf($banned, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Install-Loose.ps1 uses '$banned', which Constrained Language Mode blocks on Windows 10 ARM32 15035"
        }
    }
    if ($code -notmatch 'AppxSignature\.p7x') { throw 'Install-Loose.ps1 does not remove AppxSignature.p7x' }
    if ($code -notmatch 'Get-AppxPackage') { throw 'Install-Loose.ps1 does not check for an existing installation' }
    if ($code -notmatch 'Remove-AppxPackage') { throw 'Install-Loose.ps1 does not explain how to remove the old copy' }

    # The save folder name ends with a certificate hash, so the batch looks it up
    # instead of hard-coding it; only the prefix and the LocalState part must be there.
    if ($texts['Open-Saves.bat'] -notmatch 'BadPiggiesRebooted_' -or $texts['Open-Saves.bat'] -notmatch 'LocalState') {
        throw 'Open-Saves.bat does not look up the package save folder'
    }

    return $LooseFiles
}

function Find-PublishedArm32Appx {
    # The ARM32 release is published as Bad_PiggiesRebooted_<version>_ARM32.appx.
    # Packages published before the rename are still called Bad PiggiesRebooted_..._ARM.appx,
    # so both spellings are accepted, newest name first.
    $folder = Join-Path $PublishRoot 'BPRE_UWP-ARM32'
    foreach ($pattern in @('*_ARM32.appx', '*_ARM.appx')) {
        $found = @(Get-ChildItem -LiteralPath $folder -Filter $pattern -File -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending)
        if ($found.Count -gt 0) { return $found[0].FullName }
    }
    return $null
}

function Invoke-SourcePackageBuild {
    $builder = Join-Path $PSScriptRoot 'Build-UWP-ARM32.ps1'
    Assert-Path -Path $builder -Label 'Build-UWP-ARM32.ps1'

    Write-Step 'Source package is missing - building UWP ARM32 first'
    Write-Host "  This runs Build-UWP-ARM32.ps1 (Unity build + MSBuild + publish, several minutes)."

    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $builder, '-NoPause')
    if ($UnityPath) { $arguments += @('-UnityPath', $UnityPath) }
    if ($MSBuildPath) { $arguments += @('-MSBuildPath', $MSBuildPath) }

    $output = Invoke-Native -FilePath 'powershell.exe' -Arguments $arguments -What 'Build-UWP-ARM32.ps1'
    foreach ($line in $output) { if ("$line".Trim()) { Write-Host "  $line" } }
    Write-Ok 'UWP ARM32 build finished'
}

function Update-Manifest {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = [System.IO.File]::ReadAllText($Path)
    $pattern = '[ \t]*<PackageDependency\s+Name="Microsoft\.VCLibs\.140\.00"[^>]*/>[ \t]*\r?\n'
    $patched = [regex]::Replace($text, $pattern, '')
    if ($patched -eq $text) {
        throw ('the Microsoft.VCLibs.140.00 PackageDependency was not found in {0} - is this the right package?' -f (Split-Path -Leaf $Path))
    }
    if ($patched -match 'Microsoft\.VCLibs') {
        throw 'the manifest still mentions Microsoft.VCLibs after removing the PackageDependency'
    }

    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $patched, $utf8)
    return $true
}

function Get-AppxSignatureThumbprint {
    param([Parameter(Mandatory = $true)][string]$Path)

    Add-Type -AssemblyName System.Security -ErrorAction Stop
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop

    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -eq 'AppxSignature.p7x' } | Select-Object -First 1
        if (-not $entry) { throw "no AppxSignature.p7x in $Path" }
        $stream = $entry.Open()
        try {
            $memory = New-Object System.IO.MemoryStream
            $stream.CopyTo($memory)
            $signature = $memory.ToArray()
        } finally { $stream.Dispose() }
    } finally { $zip.Dispose() }

    # AppX wraps the CMS blob in the "PKCX" signature header (4 bytes).
    if ($signature.Length -lt 8) { throw 'the AppX signature is too short' }
    $der = New-Object byte[] ($signature.Length - 4)
    [System.Array]::Copy($signature, 4, $der, 0, $der.Length)

    $cms = New-Object System.Security.Cryptography.Pkcs.SignedCms
    $cms.Decode($der)
    if ($cms.SignerInfos.Count -lt 1) { throw 'the AppX signature has no signer' }
    return $cms.SignerInfos[0].Certificate.Thumbprint
}

function Get-AppxEntryBytes {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$EntryName
    )

    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -eq $EntryName } | Select-Object -First 1
        if (-not $entry) {
            # OPC part names are percent-encoded: "Bad PiggiesRebooted.exe" is
            # stored as "Bad%20PiggiesRebooted.exe".
            $encodedName = $EntryName.Replace(' ', '%20')
            $entry = $zip.Entries | Where-Object { $_.FullName -eq $encodedName } | Select-Object -First 1
        }
        if (-not $entry) { return $null }
        $stream = $entry.Open()
        try {
            $memory = New-Object System.IO.MemoryStream
            $stream.CopyTo($memory)
            # The comma stops PowerShell from unrolling the array into single bytes.
            return ,$memory.ToArray()
        } finally { $stream.Dispose() }
    } finally { $zip.Dispose() }
}

function Get-AppxEntryNames {
    param([Parameter(Mandatory = $true)][string]$Path)

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        return @($zip.Entries | ForEach-Object { $_.FullName })
    } finally { $zip.Dispose() }
}

function Test-PackedBinary {
    # Re-reads a payload file from the finished appx and proves the import rewrite
    # survived packing.
    param(
        [Parameter(Mandatory = $true)][string]$Package,
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)]$Renames
    )

    $bytes = Get-AppxEntryBytes -Path $Package -EntryName $FileName
    if (-not $bytes) { throw "$FileName is missing from the packed appx" }
    $info = Get-PeInfo -Bytes $bytes
    $imports = @(Get-PeImports -Info $info)

    $bad = @($imports | Where-Object { $_.ToLower().EndsWith('_app.dll') -or $_.ToLower() -eq 'd3d11.dll' })
    if ($bad.Count -gt 0) { throw ("{0} in the packed appx still imports {1}" -f $FileName, ($bad -join ', ')) }

    foreach ($new in $Renames.Values) {
        if (-not ($imports | Where-Object { $_ -ieq $new })) {
            throw ("{0} in the packed appx does not import {1}" -f $FileName, $new)
        }
    }
    return $imports
}

function Get-PackageReadme {
    param(
        [Parameter(Mandatory = $true)][int]$ShaderCount,
        [Parameter(Mandatory = $true)][string]$GameExe,
        [Parameter(Mandatory = $true)][string]$PackageName
    )

    $text = @"
========================================================================
 Bad Piggies :: Rebooted - ARM32 package for Direct3D 9_1 GPUs
 (Surface RT, Surface 2, Windows 10 ARM32 build 15035)
========================================================================

This is the same game as in BPRE_UWP-ARM32, with one addition: a small
Direct3D 11 -> 9_1 layer ($ShimName) that lets the game run on the GPU of
Surface RT / Surface 2 instead of falling back to software rendering.

$("  {0,-54} {1}" -f $PackageName, 'the game (contains everything below)')
$("  {0,-54} {1}" -f 'BPRE_UWP.cer', 'the signing certificate')
$("  {0,-54} {1}" -f 'Install.bat / Install.ps1', 'automatic installer (desktop)')
$("  {0,-54} {1}" -f 'Open-Saves.bat', 'opens the save folder (back up by hand)')
$("  {0,-54} {1}" -f 'README.txt', 'this file')
$("  {0,-54} {1}" -f 'PATCH_REPORT.txt', 'exactly what was changed')

The file name ends in _ARM32_TegraPatch so it cannot be mixed up with the
normal ARM32 package (Bad_PiggiesRebooted_<version>_ARM32.appx). The package
identity inside is unchanged, so installing this build replaces the normal
ARM32 release and vice versa.

Inside the appx the same Open-Saves.bat is stored again, next to the scripts
for the unpacked variant: Install-Loose.bat / .ps1 (registers an unpacked
folder). See "INSTALLING FROM AN UNPACKED COPY" below.

There is no Dependencies folder: the Visual C++ runtime is inside the
package, so Microsoft.VCLibs.140.00 is not needed on the device.

Supported devices
  Surface RT and Surface 2 running Windows 10 ARM32 build 15035
  (the leaked 32-bit ARM desktop build). The package also installs on
  Windows 10/11 ARM64 through ARM32 compatibility, but the shim only
  helps a real feature-level 9_1 GPU.

The package has the same name, version and publisher as the regular
ARM32 build, so installing it replaces that build (and vice versa).

HOW TO INSTALL
  1. Settings -> Update & security -> For developers -> Developer mode.
  2. Copy this whole folder to the tablet (USB stick, network share...).
  3. Run Install.bat. Confirm the administrator prompt - it is needed to
     trust the certificate.
  4. Start "Bad Piggies::Rebooted" from the Start menu.

If Install.bat refuses to start, open an administrator PowerShell window
in this folder and run:
  powershell -ExecutionPolicy Bypass -File .\Install.ps1

WHAT WAS CHANGED IN THE PACKAGE
  * l9d3d11.dll                  the Direct3D feature-level 9_1 shim
                                 (UnityPlayer loads it instead of d3d11.dll)
  * Data\bp\l9_replacements.bin  $ShaderCount level-9 shader replacements
  * vcruntime140.dll, msvcp140*.dll, vccorlib140.dll, concrt140.dll
                                 the Visual C++ runtime, app-local
  * AppxManifest.xml             the Microsoft.VCLibs.140.00 dependency
                                 was removed (the runtime now ships inside)
  * UnityPlayer.dll / GameAssembly.dll / $GameExe
                                 imports repointed (d3d11 -> l9d3d11.dll,
                                 *_APP runtime -> app-local DLLs).
                                 Nothing else in the game was touched.

INSTALLING FROM AN UNPACKED COPY
  The appx can also be unpacked into a folder and registered from there -
  no certificate, only Developer Mode:
    1. Unpack it (rename the .appx to .zip and extract, or use
       "makeappx unpack") into a normal folder such as C:\BadPiggies.
       Not the root of a drive: Windows cannot register an app from there.
    2. Run Install-Loose.bat in that folder.
    3. Keep the folder where it is - Windows runs the game from it.
  When 7-Zip, Explorer or "Extract All" was used, the exe comes out as
  "Bad%20PiggiesRebooted.exe" and one folder as
  "unity%20default%20resources": those tools do not decode the names stored
  inside an .appx. Install-Loose.bat renames them back automatically, so no
  manual renaming is needed ("makeappx unpack" decodes them already).
  Install-Loose.bat refuses when the game is already installed, because
  Windows cannot replace an installed copy from a loose folder. Back the
  saves up first with Open-Saves.bat, remove the game by hand (Settings ->
  Apps -> Bad Piggies::Rebooted -> Uninstall) and run it again.
  Open-Saves.bat opens the save folder in Explorer at any time; copy that
  folder to keep Progress.dat, Settings.xml, playerprefs.dat and
  contraptionsB\. It is
    %LOCALAPPDATA%\Packages\BadPiggiesRebooted_<hash>\LocalState
  where <hash> comes from the signing certificate: x0xr9mv5edyrp for this
  build, something else (for example ...hj) for a package signed with another
  certificate. That is why the script looks the folder up instead of using a
  fixed name. Removing the app deletes the saves, so back them up before
  uninstalling or reinstalling.

IF IT DOES NOT START OR LOOKS WRONG
  Send back the file  l9shim.log  - it is written to %LOCALAPPDATA% and, inside
  the package, ends up in the app's own store:
    %LOCALAPPDATA%\Packages\BadPiggiesRebooted_<hash>\...\l9shim.log
  (search for l9shim.log in that folder). It records the GPU, the feature
  level the shim got, how many shaders it swapped and any error. That file
  says exactly what to fix. The regular Unity log is next to it.

TROUBLESHOOTING
  0x800B0109 / 0x800B010A  the certificate is not trusted. Run Install.bat
                           as administrator (it imports the .cer into
                           Trusted People) and retry.
  0x80073CF3               a package dependency is missing. This build has
                           no dependencies - you are probably installing a
                           different (non-L9) package from another folder.
  0x80073CFB / 0x80073D06  this or a newer version is installed. Remove it
                           first: Settings -> Apps -> Bad Piggies::Rebooted.

Ported by MaxRM, TNT ENTERTAINMENT Inc.
Bad Piggies is a trademark of Rovio Entertainment. This port is a
non-commercial preservation project and is not affiliated with Rovio.
========================================================================
"@
    return $text.Replace("`r`n", "`n").Replace("`n", "`r`n")
}

# ---------------------------------------------------------------------------
# main
# ---------------------------------------------------------------------------

try {
    Write-Step 'Preparing the L9D3D11 package'
    Write-Host "  Fix folder: $FixRoot"

    Assert-Path -Path (Join-Path $FixRoot 'src\l9shim.c') -Label 'shim source'
    Assert-Path -Path $VcLibsFolder -Label 'app-local VC++ runtime folder'
    Assert-Path -Path (Join-Path $PSScriptRoot 'Build-Common.ps1') -Label 'Build-Common.ps1'

    $makeappx = Get-SdkTool -Name 'makeappx'
    $signtool = Get-SdkTool -Name 'signtool'
    Assert-Path -Path $UwpCertificatePfx -Label 'signing certificate'
    Write-Ok "makeappx: $makeappx"
    Write-Ok "signtool: $signtool"

    # 1. shim
    if ($ForceShimRebuild -or -not (Test-Path -LiteralPath $ShimPath)) {
        Invoke-ShimBuild -Force:$ForceShimRebuild
    } else {
        Write-Ok "Using the shipped shim: $ShimPath"
    }
    $shim = Assert-Shim -Path $ShimPath
    $shimSha = Get-Sha256 -Path $ShimPath
    Add-Report 'Bad Piggies Rebooted - UWP ARM32 package with the l9d3d11 Direct3D 9_1 shim'
    Add-Report ''
    Add-Report ("shim        {0}: {1} bytes, sha256 {2}" -f $ShimName, $shim.Size, $shimSha)
    Add-Report ("            exports: {0}" -f ($shim.Exports -join ', '))
    Add-Report '            imports: KERNEL32.dll'

    $shaderTable = Assert-ShaderTable -Path $TablePath
    $tableSha = Get-Sha256 -Path $TablePath
    Add-Report ("shader table {0}: {1} entries, {2} bytes, sha256 {3}" -f $TableRelPath, $shaderTable.Count, $shaderTable.Size, $tableSha)

    # 2. source package
    if (-not $SourceAppx) { $SourceAppx = Find-PublishedArm32Appx }
    if (-not $SourceAppx -or -not (Test-Path -LiteralPath $SourceAppx)) {
        if ($SourceAppx) {
            Write-Warn "source package not found: $SourceAppx"
        } else {
            Write-Warn "no ARM32 package in $PublishRoot\BPRE_UWP-ARM32"
        }
        Invoke-SourcePackageBuild
        if (-not $AppxPath) { $SourceAppx = Find-PublishedArm32Appx }
    }
    Assert-Path -Path $SourceAppx -Label 'source appx (run Build-UWP-ARM32.ps1 first)'
    $SourceAppx = (Resolve-Path -LiteralPath $SourceAppx).Path
    $sourceSha = Get-Sha256 -Path $SourceAppx
    Write-Ok "Source package: $SourceAppx"
    Add-Report ''
    Add-Report ("source      {0}" -f $SourceAppx)
    Add-Report ("            sha256 {0}" -f $sourceSha)

    # 3. unpack
    Write-Step 'Unpacking the source package'
    if (Test-Path -LiteralPath $StageRoot) { Remove-Item -LiteralPath $StageRoot -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $Stage | Out-Null

    $null = Invoke-Native -FilePath $makeappx -Arguments @('unpack', '/p', $SourceAppx, '/d', $Stage, '/o') -What 'makeappx unpack'
    Assert-Path -Path (Join-Path $Stage 'AppxManifest.xml') -Label 'AppxManifest.xml (is this a UWP appx?)'

    # The game exe keeps the name the project gives it (a rebuild can insert a
    # space), so it is taken from the manifest - every check and rename below
    # uses this name.
    $manifestPath = Join-Path $Stage 'AppxManifest.xml'
    $manifestText = [System.IO.File]::ReadAllText($manifestPath)
    if ($manifestText -match '<Application\b[^>]*\bExecutable="([^"]+)"') {
        $GameExe = [System.IO.Path]::GetFileName($Matches[1])
    } else {
        throw 'AppxManifest.xml has no <Application Executable="..."> entry'
    }
    if (-not $GameExe) { throw 'AppxManifest.xml declares an empty application executable' }

    foreach ($name in @('UnityPlayer.dll', 'GameAssembly.dll', $GameExe)) {
        Assert-Path -Path (Join-Path $Stage $name) -Label $name
    }
    Write-Ok "Unpacked to $Stage"
    Write-Ok "Game executable: $GameExe"

    $ImportRenames = [ordered]@{}
    $ImportRenames[$GameExe] = $GameExeRenames
    foreach ($name in $BinaryRenames.Keys) { $ImportRenames[$name] = $BinaryRenames[$name] }
    Add-Report ("game exe    {0}" -f $GameExe)

    # makeappx wrote these for the packed package; they must not be packed again.
    foreach ($junk in @('AppxSignature.p7x', 'AppxBlockMap.xml')) {
        $path = Join-Path $Stage $junk
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
    $metadata = Join-Path $Stage 'AppxMetadata'
    if (Test-Path -LiteralPath $metadata) { Remove-Item -LiteralPath $metadata -Recurse -Force }

    # 4. the game build must be the one the shim was tested with
    $unityPlayerPath = Join-Path $Stage 'UnityPlayer.dll'
    $unityPlayerSha = Get-Sha256 -Path $unityPlayerPath
    if ($unityPlayerSha -ne $TestedUnityPlayerSha) {
        if (-not $AllowDifferentUnityPlayer) {
            throw ("UnityPlayer.dll differs from the build the shim was verified against.`n" +
                   "  expected: $TestedUnityPlayerSha`n" +
                   "  found:    $unityPlayerSha`n" +
                   "The shim's capability patches are bound to RVAs of the tested DLL and are silently`n" +
                   "disabled on anything else, so the game may render incorrectly. Rebuild the package`n" +
                   "from the tested game, or pass -AllowDifferentUnityPlayer if you know what you are doing.")
        }
        Write-Warn 'UnityPlayer.dll differs from the tested build (-AllowDifferentUnityPlayer)'
    } else {
        Write-Ok 'UnityPlayer.dll matches the tested build'
    }
    Add-Report ("UnityPlayer.dll sha256 {0} ({1})" -f $unityPlayerSha,
        $(if ($unityPlayerSha -eq $TestedUnityPlayerSha) { 'tested build' } else { 'NOT the tested build' }))

    # 5. import repointing
    Write-Step 'Repointing imports'
    $shimExports = @(Get-PeExports -Info (Get-PeInfo -Bytes ([System.IO.File]::ReadAllBytes($ShimPath))))

    foreach ($fileName in $ImportRenames.Keys) {
        $path = Join-Path $Stage $fileName
        $renames = $ImportRenames[$fileName]
        $before = Get-PeInfo -Bytes ([System.IO.File]::ReadAllBytes($path))

        if ($fileName -eq 'UnityPlayer.dll') {
            # The shim only forwards these three entry points; a fourth one would
            # fail at launch time with "entry point not found".
            $needed = @(Get-PeImportedFunctions -Info $before -DllName 'd3d11.dll')
            $missing = @($needed | Where-Object { $name = $_; -not ($shimExports | Where-Object { $_ -ceq $name }) })
            if ($missing.Count -gt 0) {
                throw ("UnityPlayer.dll imports d3d11 functions the shim does not export: {0}" -f ($missing -join ', '))
            }
            Write-Host ("  d3d11 entry points used: {0}" -f ($needed -join ', '))
        }

        $renameMap = @{}
        foreach ($old in $renames.Keys) { $renameMap[$old] = $renames[$old] }
        $changed = Update-PeImportFile -Path $path -Renames $renameMap

        foreach ($change in $changed) {
            Write-Ok ("{0}: {1} -> {2}" -f $fileName, $change.Old, $change.New)
            Add-Report ("{0}: import {1} -> {2} (name RVA {3:X8})" -f $fileName, $change.Old, $change.New, $change.NameRva)
        }
    }

    # 6. payload: shim, runtime, shader table
    Write-Step 'Adding the shim, the runtime and the shader table'
    Copy-Item -LiteralPath $ShimPath -Destination (Join-Path $Stage $ShimName) -Force
    Write-Ok "$ShimName ($($shim.Size) bytes)"

    foreach ($dll in $VcLibs) {
        $source = Join-Path $VcLibsFolder $dll
        Assert-Path -Path $source -Label "app-local runtime $dll"
        Copy-Item -LiteralPath $source -Destination (Join-Path $Stage $dll) -Force
    }
    Write-Ok ("VC++ runtime (app-local): {0}" -f ($VcLibs -join ', '))
    Add-Report ("VC++ runtime (app-local, from Visual Studio 14.44 onecore\arm): {0}" -f ($VcLibs -join ', '))

    $bpDir = Join-Path (Join-Path $Stage 'Data') 'bp'
    New-Item -ItemType Directory -Force -Path $bpDir | Out-Null
    Copy-Item -LiteralPath $TablePath -Destination (Join-Path $Stage $TableRelPath) -Force
    Write-Ok "$TableRelPath ($($shaderTable.Size) bytes)"

    # 6b. installers for the unpacked variant. The appx stays a normal appx; these
    #     files only matter after somebody unpacks it and registers the folder.
    Write-Step 'Adding the unpacked-install scripts'
    $null = Assert-LooseFiles -Folder $LooseDir
    foreach ($name in $LooseFiles) {
        Copy-Item -LiteralPath (Join-Path $LooseDir $name) -Destination (Join-Path $Stage $name) -Force
    }
    Write-Ok ("{0} files: {1}" -f $LooseFiles.Count, ($LooseFiles -join ', '))
    Add-Report ("unpacked install: {0}" -f ($LooseFiles -join ', '))

    # 7. manifest
    Write-Step 'Patching AppxManifest.xml'
    $manifestPath = Join-Path $Stage 'AppxManifest.xml'
    $null = Update-Manifest -Path $manifestPath
    Write-Ok 'Microsoft.VCLibs.140.00 PackageDependency removed (runtime is app-local now)'
    Add-Report 'AppxManifest.xml: removed the Microsoft.VCLibs.140.00 PackageDependency'

    # 15035 is the ARM32 desktop build of Surface RT / Surface 2. The manifest only has to
    # ask for something that old or older; Unity writes 10.0.10240, which installs fine.
    $manifestText = [System.IO.File]::ReadAllText($manifestPath)
    if ($manifestText -match '<TargetDeviceFamily\b[^>]*\bMinVersion="([\d.]+)"') {
        $minVersion = [version]$Matches[1]
        if ($minVersion -gt [version]'10.0.15035.0') {
            Write-Warn "manifest MinVersion $minVersion is newer than 10.0.15035 - Surface RT / Surface 2 will refuse it"
        } else {
            Write-Ok "manifest MinVersion $minVersion - installable on 10.0.15035"
        }
        Add-Report ("manifest    MinVersion {0} (Surface RT runs 10.0.15035)" -f $minVersion)
    } else {
        Write-Warn 'AppxManifest.xml has no TargetDeviceFamily MinVersion'
    }

    # 8. repack under the release name
    Write-Step 'Packing the appx'
    #
    # The published file says what it is: the source identity with spaces replaced by
    # underscores plus a Tegra/9_1 marker, so it can never be confused with the stock
    # ARM32 package:
    #   Bad PiggiesRebooted_1.0.0.0_ARM32.appx -> Bad_PiggiesRebooted_1.0.0.0_ARM32_TegraPatch.appx
    # Only the file name changes; the package identity, version and publisher inside are
    # the same, so this build replaces (and is replaced by) the normal ARM32 release.
    $sourceBase = [System.IO.Path]::GetFileNameWithoutExtension($SourceAppx)
    $outBase = [regex]::Replace($sourceBase, '\s+', '_')
    $outBase = [regex]::Replace($outBase, '_ARM(?:32)?$', '_ARM32_TegraPatch')
    if ($outBase -notmatch '_ARM32_TegraPatch$') { $outBase += '_ARM32_TegraPatch' }
    $outName = "$outBase.appx"
    $outAppx = Join-Path $StageRoot $outName
    if (Test-Path -LiteralPath $outAppx) { Remove-Item -LiteralPath $outAppx -Force }
    $null = Invoke-Native -FilePath $makeappx -Arguments @('pack', '/d', $Stage, '/p', $outAppx, '/o') -What 'makeappx pack'
    Assert-Path -Path $outAppx -Label 'repacked appx'
    Write-Ok ("Packed: {0} ({1:N1} MB)" -f $outName, ((Get-Item -LiteralPath $outAppx).Length / 1MB))

    # 9. sign
    Write-Step 'Signing the package'
    $null = Invoke-Native -FilePath $signtool -Arguments @(
        'sign', '/fd', 'SHA256', '/f', $UwpCertificatePfx, '/p', $UwpCertificatePassword,
        '/d', 'Bad Piggies::Rebooted', $outAppx
    ) -What 'signtool sign'
    Write-Ok "Signed with $UwpPublisher"

    $pfxCertificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($UwpCertificatePfx, $UwpCertificatePassword)
    $signatureThumbprint = Get-AppxSignatureThumbprint -Path $outAppx
    if ($signatureThumbprint -ne $pfxCertificate.Thumbprint) {
        throw ("the package is signed by {0}, expected {1}" -f $signatureThumbprint, $pfxCertificate.Thumbprint)
    }
    Write-Ok "Signature verified: $signatureThumbprint"
    Add-Report ("signature   subject {0}, thumbprint {1}" -f $pfxCertificate.Subject, $signatureThumbprint)

    # 10. verify the packed payload, not the staging folder
    Write-Step 'Verifying the packed package'
    $entries = Get-AppxEntryNames -Path $outAppx
    $required = @($ShimName, $TableRelPath.Replace('\', '/')) + $VcLibs + $LooseFiles
    $missing = @($required | Where-Object { $name = $_; -not ($entries -contains $name) })
    if ($missing.Count -gt 0) { throw ("packed appx is missing: {0}" -f ($missing -join ', ')) }
    Write-Ok ("payload present: {0} files added to the package" -f $required.Count)

    foreach ($name in $LooseFiles) {
        $bytes = Get-AppxEntryBytes -Path $outAppx -EntryName $name
        if (-not $bytes -or $bytes.Length -eq 0) { throw "$name in the packed appx is empty" }
    }
    Write-Ok ("unpacked-install scripts present: {0}" -f ($LooseFiles -join ', '))

    if ($entries -contains 'd3d11.dll') { throw 'the appx contains a stray d3d11.dll' }
    $strayApp = @($entries | Where-Object { $_.ToLower().EndsWith('_app.dll') })
    if ($strayApp.Count -gt 0) { throw ("the appx still contains framework runtime DLLs: {0}" -f ($strayApp -join ', ')) }

    # The one asset with a space must stay percent-encoded inside an OPC package.
    $decodedAsset = @($entries | Where-Object { $_ -eq 'Data/Resources/unity default resources' })
    if ($decodedAsset.Count -gt 0) {
        throw 'Data/Resources/unity default resources is not percent-encoded in the appx - Unity would not load its builtin shaders'
    }

    $packedManifest = Get-AppxEntryBytes -Path $outAppx -EntryName 'AppxManifest.xml'
    $packedManifestText = [System.Text.Encoding]::UTF8.GetString($packedManifest)
    if ($packedManifestText -match 'Microsoft\.VCLibs') { throw 'the packed manifest still references Microsoft.VCLibs' }
    if ($packedManifestText -notmatch '<Identity\s+Name="BadPiggiesRebooted"') { throw 'the packed manifest has an unexpected Identity' }
    Write-Ok 'manifest: no VCLibs dependency, identity unchanged'

    foreach ($fileName in $ImportRenames.Keys) {
        $imports = Test-PackedBinary -Package $outAppx -FileName $fileName -Renames $ImportRenames[$fileName]
        $names = @($imports | Where-Object { $_.ToLower() -notlike 'api-ms-*' -and $_.ToLower() -notlike 'ext-ms-*' })
        Add-Report ("  {0,-26} {1}" -f $fileName, ($names -join ' '))
    }
    Write-Ok 'imports repointed in the packed payload'

    $packedShim = Get-AppxEntryBytes -Path $outAppx -EntryName $ShimName
    if (-not $packedShim -or $packedShim.Length -ne $shim.Size) { throw 'the packed l9d3d11.dll is missing or truncated' }
    if ((Get-PeInfo -Bytes $packedShim).Machine -ne 0x1C4) { throw 'the packed l9d3d11.dll is not ARM32' }
    Write-Ok "$ShimName in the package: ARM32, $($packedShim.Length) bytes"

    $outSha = Get-Sha256 -Path $outAppx
    Add-Report ''
    Add-Report ("package     {0}" -f $outName)
    Add-Report ("            sha256 {0}" -f $outSha)

    # 11. publish
    $finalAppx = $outAppx
    if ($NoPublish) {
        Write-Step 'Publishing skipped (-NoPublish)'
        Write-Host "  Signed package kept in the stage folder: $outAppx"
    } else {
        Write-Step 'Publishing'
        if (Test-Path -LiteralPath $Destination) { Remove-Item -LiteralPath $Destination -Recurse -Force }
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null

        $published = New-Object System.Collections.Generic.List[object]
        $targetAppx = Join-Path $Destination $outName
        Copy-Item -LiteralPath $outAppx -Destination $targetAppx -Force
        $published.Add((Get-Item -LiteralPath $targetAppx))

        $cer = [System.IO.Path]::ChangeExtension($UwpCertificatePfx, '.cer')
        if (Test-Path -LiteralPath $cer) {
            $targetCer = Join-Path $Destination (Split-Path -Leaf $cer)
            Copy-Item -LiteralPath $cer -Destination $targetCer -Force
            $published.Add((Get-Item -LiteralPath $targetCer))
        } else {
            Write-Warn "the .cer file next to the .pfx was not found: $cer"
        }

        foreach ($name in @('Install.bat', 'Install.ps1', 'Open-Saves.bat')) {
            $source = Join-Path $UwpInstallDir $name
            Assert-Path -Path $source -Label "installer file $name"
            $target = Join-Path $Destination $name
            Copy-Item -LiteralPath $source -Destination $target -Force
            $published.Add((Get-Item -LiteralPath $target))
        }

        $readmeTarget = Join-Path $Destination 'README.txt'
        $readme = Get-PackageReadme -ShaderCount ([int]$shaderTable.Count) -GameExe $GameExe -PackageName $outName
        $ascii = New-Object System.Text.ASCIIEncoding
        [System.IO.File]::WriteAllText($readmeTarget, $readme, $ascii)
        $published.Add((Get-Item -LiteralPath $readmeTarget))

        $reportTarget = Join-Path $Destination 'PATCH_REPORT.txt'
        $reportText = (($Report | ForEach-Object { "$_" }) -join "`r`n") + "`r`n"
        [System.IO.File]::WriteAllText($reportTarget, $reportText, $ascii)
        $published.Add((Get-Item -LiteralPath $reportTarget))

        Write-Ok "Published: $Destination"
        foreach ($file in $published) {
            Write-Host ("    {0,-45} {1,12:N0} bytes" -f $file.Name, $file.Length)
        }
        $finalAppx = $targetAppx
    }

    Write-Step 'Done'
    Write-Host "  Package: $finalAppx"
    Write-Host "  sha256:  $outSha"
    Write-Host ''
    if ($NoPublish) {
        Write-Host '  Nothing was copied to Publish\ (-NoPublish); the stage folder is kept.' -ForegroundColor Yellow
    } else {
        Write-Host '  Install it on the tablet with Install.bat from the published folder.' -ForegroundColor Green
    }
    Write-Host '  If it renders wrongly, send back l9shim.log: it is written to %LOCALAPPDATA%' -ForegroundColor Green
    Write-Host '  and ends up under %LOCALAPPDATA%\Packages\BadPiggiesRebooted_<hash>\.' -ForegroundColor Green

    $succeeded = $true
} catch {
    if (-not $NoPause) {
        Write-Failure -Message $_.Exception.Message
        Wait-ForKeyPress
    }
    throw
} finally {
    if ($succeeded -and -not $KeepStage -and -not $NoPublish) {
        Remove-Item -LiteralPath $StageRoot -Recurse -Force -ErrorAction SilentlyContinue
    } elseif (-not $succeeded) {
        Write-Host ''
        Write-Host "  Unpacked stage kept for inspection: $Stage" -ForegroundColor Yellow
    }
}
