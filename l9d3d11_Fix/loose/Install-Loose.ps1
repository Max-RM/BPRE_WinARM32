# Installs Bad Piggies::Rebooted from this unpacked copy of the package.
#
# This is the alternative to the signed .appx: the .appx was unpacked into a normal
# folder and Windows runs the game straight from it (Add-AppxPackage -Register).
# Nothing has to be trusted (no certificate), but Developer Mode must be on and the
# folder must stay where it is after registration.
#
# Called by Install-Loose.bat, which can also be double-clicked:
#     powershell -ExecutionPolicy Bypass -File .\Install-Loose.ps1
#
# Written for Windows 10 ARM32 build 15035 in Constrained Language Mode: no Add-Type,
# no System.IO.Compression, no New-Object and no .NET method calls.

param(
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

# Fixed identity of every package produced by this port (see AppxManifest.xml).
$packageName = 'BadPiggiesRebooted'
# Save folder of this package. The family name ends with a hash of the signing
# certificate: x0xr9mv5edyrp for the TNT ENTERTAINMENT certificate, something
# else (for example ...hj) for a build signed with another one. The real name is
# asked from Get-AppxPackage whenever the package is known; these two lines are
# only the fallback for the messages printed before that.
$saveFolder  = Join-Path $env:LOCALAPPDATA 'Packages\BadPiggiesRebooted_x0xr9mv5edyrp\LocalState'
$logPath     = Join-Path $here 'appxlog.txt'

function Write-Step {
    param([string]$Text)
    Write-Host ''
    Write-Host "=== $Text ===" -ForegroundColor Cyan
}

function Write-Ok   { param([string]$Text) Write-Host "  OK: $Text"      -ForegroundColor Green }
function Write-Warn { param([string]$Text) Write-Host "  WARNING: $Text" -ForegroundColor Yellow }
function Write-Err  { param([string]$Text) Write-Host "  ERROR: $Text"   -ForegroundColor Red }
function Write-Note { param([string]$Text) Write-Host "  $Text" }

# --- OPC file names ----------------------------------------------------------
#
# An .appx is an OPC/ZIP container, and ZIP entry names cannot hold every
# character, so some are percent-encoded inside the package: the game exe is
# stored as "Bad%20PiggiesRebooted.exe" and one data folder as
# "unity%20default%20resources". makeappx unpack and the AppX runtime decode
# that, but 7-Zip, Explorer and most other unpackers keep the raw name.
# AppxManifest.xml says Executable="Bad PiggiesRebooted.exe", so such a folder
# cannot be registered and the player never finds its resources folder.
# These two functions rename those entries back before anything is registered.
# Only string arithmetic is used, so they also run in Constrained Language Mode.

$hexValues = @{
    '0' = 0;  '1' = 1;  '2' = 2;  '3' = 3;  '4' = 4;  '5' = 5;  '6' = 6;  '7' = 7
    '8' = 8;  '9' = 9;  'A' = 10; 'B' = 11; 'C' = 12; 'D' = 13; 'E' = 14; 'F' = 15
}

function ConvertFrom-OpcName {
    param([string]$Name)

    $text = [string]$Name
    if ($text -notmatch '%[0-9A-Fa-f]{2}') { return $text }

    $result = ''
    $index = 0
    while ($index -lt $text.Length) {
        $character = [string]$text[$index]
        if ($character -eq '%' -and ($index + 2) -lt $text.Length) {
            # The lookup is case-insensitive, so "%2f" and "%2F" both work.
            $high = ([string]$text[$index + 1]).ToUpperInvariant()
            $low  = ([string]$text[$index + 2]).ToUpperInvariant()
            if ($hexValues.ContainsKey($high) -and $hexValues.ContainsKey($low)) {
                $code = $hexValues[$high] * 16 + $hexValues[$low]
                $result = $result + [char]$code
                $index = $index + 3
                continue
            }
        }
        $result = $result + $character
        $index = $index + 1
    }
    return $result
}

function Repair-OpcNames {
    param([string]$Root)

    $items = @(Get-ChildItem -LiteralPath $Root -Recurse -Force -ErrorAction SilentlyContinue)
    # Deepest paths first: renaming a folder would invalidate the paths of the
    # files that are still inside it.
    $items = @($items | Sort-Object -Property @{ Expression = { ([string]$_.FullName).Length } } -Descending)

    $count = 0
    foreach ($item in $items) {
        $name = [string]$item.Name
        if ($name -notmatch '%[0-9A-Fa-f]{2}') { continue }

        $fixed = ConvertFrom-OpcName -Name $name
        if ($fixed -eq $name) { continue }
        if ($fixed -match '[\\/:*?"<>|]') {
            Write-Warn "cannot restore the name of '$name' - it would contain a character Windows does not allow"
            continue
        }

        $parent = Split-Path -Parent $item.FullName
        if (Test-Path -LiteralPath (Join-Path $parent $fixed)) {
            Write-Warn "cannot rename '$name' - '$fixed' already exists"
            continue
        }

        Rename-Item -LiteralPath $item.FullName -NewName $fixed
        Write-Note "$name  ->  $fixed"
        $count = $count + 1
    }
    return $count
}

$exitCode = 0

try {
    Write-Host 'Bad Piggies::Rebooted - install from an unpacked folder'
    Write-Host "Folder: $here"
    Write-Host "PowerShell language mode: $($ExecutionContext.SessionState.LanguageMode)"

    # AppX deployment cannot register a package whose files sit in the root of a
    # drive (it fails with 0x80073CF9 / "cannot access deployment target at C:").
    if ($here -match '^[A-Za-z]:\\?$') {
        Write-Err 'The game was unpacked directly into the root of a drive.'
        Write-Note 'Windows cannot register an app from a drive root (error 0x80073CF9).'
        Write-Note 'Create a normal folder (for example C:\BadPiggies), move ALL of the'
        Write-Note 'unpacked files into it and run Install-Loose.bat there.'
        $exitCode = 1
    } else {
        Set-Location -LiteralPath $here

        # A folder unpacked with 7-Zip/Explorer still has "Bad%20PiggiesRebooted.exe"
        # instead of "Bad PiggiesRebooted.exe"; fix that before anything is checked.
        Write-Step 'Checking file names left percent-encoded by unpackers'
        $fixedNames = Repair-OpcNames -Root $here
        if ($fixedNames -gt 0) {
            Write-Ok "restored $fixedNames name(s) to what AppxManifest.xml expects"
        } else {
            Write-Ok 'nothing to fix - names are already correct'
        }

        $manifest = Join-Path $here 'AppxManifest.xml'
        if (-not (Test-Path -LiteralPath $manifest)) {
            throw 'AppxManifest.xml is missing - unpack the whole .appx into this folder first.'
        }
        # The game exe is named after the Unity product, which can be
        # BadPiggiesRebooted.exe or Bad PiggiesRebooted.exe, so match it by name.
        $executables = @(Get-ChildItem -LiteralPath $here -Filter '*Piggies*.exe' -File -ErrorAction SilentlyContinue)
        if ($executables.Count -eq 0) {
            throw 'the game executable (*Piggies*.exe) is missing - this is not a complete unpacked copy of the package.'
        }
        $required = @(
            'UnityPlayer.dll',
            'GameAssembly.dll',
            'l9d3d11.dll',
            'Data\bp\l9_replacements.bin'
        )
        foreach ($name in $required) {
            if (-not (Test-Path -LiteralPath (Join-Path $here $name))) {
                throw "$name is missing - this is not a complete unpacked copy of the package."
            }
        }

        # Add-AppxPackage -Register needs Developer Mode or at least sideloading.
        $unlock = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue
        if (-not $unlock -or "$($unlock.AllowDevelopmentWithoutDevLicense)" -ne '1') {
            Write-Warn 'Developer Mode may be off.'
            Write-Note 'Enable it in Settings -> Update & security -> For developers -> Developer mode.'
            Write-Note 'Without it registration usually fails with 0x80073CFF.'
            Write-Host ''
        }

        Write-Step 'Checking for an existing installation'
        $installed = @(Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue)

        if ($installed.Count -gt 0) {
            $current = $installed[0]
            if ($current.PackageFamilyName) {
                $saveFolder = Join-Path $env:LOCALAPPDATA "Packages\$($current.PackageFamilyName)\LocalState"
            }
            Write-Warn 'Bad Piggies::Rebooted is already installed:'
            Write-Note "version: $($current.Version)"
            Write-Note "package: $($current.PackageFullName)"
            if ($current.InstallLocation) { Write-Note "folder:  $($current.InstallLocation)" }
            Write-Host ''
            Write-Host '  An unpacked copy cannot replace or update an installed copy -' -ForegroundColor Yellow
            Write-Host '  Windows refuses that with 0x80073CFB / 0x80073D06.' -ForegroundColor Yellow
            Write-Host ''
            Write-Host '  To reinstall or update the game, remove the installed copy manually first:'
            Write-Note 'Settings -> Apps (Apps & features) -> Bad Piggies::Rebooted -> Uninstall'
            Write-Note 'or, in PowerShell:  Get-AppxPackage BadPiggiesRebooted | Remove-AppxPackage'
            Write-Host ''
            Write-Host '  Removing the app also deletes its save data, so back the saves up FIRST:'
            Write-Note $saveFolder
            Write-Note 'Run Open-Saves.bat next to this script, then copy the whole folder it'
            Write-Note 'opens to the Desktop, a USB stick or a network share.'
            Write-Host ''
            Write-Host '  After the old copy is gone, run Install-Loose.bat again.'
            $exitCode = 1
        } else {
            Write-Ok 'Nothing installed yet.'

            Write-Step 'Removing the packaged-install metadata'
            Write-Note 'A signed package carries a signature, a block map and content types.'
            Write-Note 'They describe the original .appx bytes and make -Register fail here,'
            Write-Note 'so they are removed. The game files themselves are not touched.'
            foreach ($name in @('AppxSignature.p7x', 'AppxBlockMap.xml', '[Content_Types].xml', 'StoreManifest.xml')) {
                $path = Join-Path $here $name
                if (Test-Path -LiteralPath $path) {
                    Remove-Item -LiteralPath $path -Force
                    Write-Ok "removed $name"
                }
            }
            $metadata = Join-Path $here 'AppxMetadata'
            if (Test-Path -LiteralPath $metadata) {
                Remove-Item -LiteralPath $metadata -Recurse -Force
                Write-Ok 'removed AppxMetadata\'
            }

            Write-Step 'Registering the game'
            try {
                Add-AppxPackage -Register $manifest -ForceApplicationShutdown -ErrorAction Stop
                Write-Ok 'Registered.'
            } catch {
                $message = $_.Exception.Message
                Write-Err $message
                Write-Host ''
                Write-Note 'Common causes:'
                Write-Note '0x80073CFF  Developer Mode / sideloading is disabled (see above).'
                Write-Note '0x80073CF9  the folder is at a drive root, read-only or on a network path.'
                Write-Note '0x800B0109  the package files were changed after unpacking - unpack again.'
                Write-Note '0x80070005  access denied - copy the folder to the local disk (not Program Files).'
                Write-Note '0x80073CFB  an older copy is still registered - remove it first.'
                Write-Host ''
                # In Constrained Language Mode the log objects cannot be piped to a
                # file at all (only core types may be written), so try only when
                # running unrestricted and always name the command as a fallback.
                if ("$($ExecutionContext.SessionState.LanguageMode)" -eq 'FullLanguage') {
                    try {
                        Get-AppxLog -ErrorAction Stop | Out-File -FilePath $logPath -Encoding utf8
                        Write-Note "Deployment log written to: $logPath"
                    } catch {
                        Write-Note 'Could not write the deployment log.'
                    }
                }
                $activity = $null
                if ($message -match '([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})') {
                    $activity = $Matches[1]
                }
                Write-Note 'For the full deployment log open PowerShell and run:'
                if ($activity) {
                    Write-Note "  Get-AppxLog -ActivityID $activity"
                } else {
                    Write-Note '  Get-AppxLog'
                }
                $exitCode = 1
            }

            if ($exitCode -eq 0) {
                $family = $null
                $now = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($now -and $now.PackageFamilyName) {
                    $family = $now.PackageFamilyName
                    $saveFolder = Join-Path $env:LOCALAPPDATA "Packages\$family\LocalState"
                }

                Write-Step 'Done'
                Write-Note 'Start "Bad Piggies::Rebooted" from the Start menu.'
                Write-Note 'Keep this folder where it is: Windows runs the game from here, so'
                Write-Note 'moving or deleting it breaks the installation.'
                Write-Host ''
                Write-Note "Saves: $saveFolder"
                Write-Note 'Back them up at any time with Open-Saves.bat.'
                Write-Note 'If the game does not start or looks wrong, send back l9shim.log:'
                Write-Note "  $saveFolder\l9shim.log"
                Write-Host ''

                if ($family) {
                    $answer = Read-Host 'Start the game now? [Y/n]'
                    if ($answer -eq '' -or $answer -match '^(y|Y|yes|YES)$') {
                        Start-Process "shell:AppsFolder\$family!App"
                    }
                }
            }
        }
    }
} catch {
    Write-Err $_.Exception.Message
    Write-Host ''
    Write-Host 'Installation failed. Read the message above, then see README-LOOSE.txt.' -ForegroundColor Yellow
    $exitCode = 1
}

if (-not $NoPause) {
    Write-Host ''
    Read-Host 'Press Enter to close this window'
}

exit $exitCode
