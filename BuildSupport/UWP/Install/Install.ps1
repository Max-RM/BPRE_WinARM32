# Installs the Bad Piggies::Rebooted UWP package that sits next to this script.
#
# Called by Install.bat, which already requested administrator rights (needed to put the
# signing certificate into Local Machine -> Trusted People). Can also be run directly:
#     powershell -ExecutionPolicy Bypass -File Install.ps1
#
# Requirements:
#   * Windows 10 version 1607 or newer (desktop), Windows 10 Mobile 10.0.15035 or newer
#   * the .appx and .cer files of one architecture in the same folder as this script
#   * Dependencies\Microsoft.VCLibs.<arch>.14.00.appx (shipped with the port)
#
# The script deliberately avoids Add-Type, System.IO.Compression and other .NET calls that
# Windows 10 ARM32 build 15035 blocks in Constrained Language Mode: the package name is known,
# and the version/architecture are read from the .appx file name instead of AppxManifest.xml.

param(
    [switch]$NoLaunchPrompt,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

# Fixed identity of every package produced by this port (see Package.appxmanifest).
$packageName = 'BadPiggiesRebooted'

function Write-Step {
    param([string]$Text)
    Write-Host ''
    Write-Host "=== $Text ===" -ForegroundColor Cyan
}

function Write-Ok    { param([string]$Text) Write-Host "  OK: $Text"         -ForegroundColor Green }
function Write-Warn2 { param([string]$Text) Write-Host "  WARNING: $Text"    -ForegroundColor Yellow }
function Write-Err   { param([string]$Text) Write-Host "  ERROR: $Text"      -ForegroundColor Red }

function Get-PackageInfo {
    param([string]$FileName)

    $version = ''
    $architecture = ''
    if ($FileName -match '_(\d+(?:\.\d+){1,3})_') { $version = $Matches[1] }
    if ($FileName -match '_(\d+(?:\.\d+){1,3})_(x86|x64|ARM|ARM64)(?:[_.]|$)') {
        $version = $Matches[1]
        $architecture = $Matches[2]
    }

    return @{ Name = $packageName; Version = $version; Architecture = $architecture }
}

function Show-HResultHint {
    param([string]$Message)

    $hints = @(
        @('0x80073CF3', 'A package dependency is missing or too old - keep the Dependencies folder next to the .appx.'),
        @('0x80073CF0', 'The package is invalid or damaged - download/copy it again.'),
        @('0x80073CFB', 'This (or a newer) version is already installed. Remove it first: Settings -> Apps -> Bad Piggies::Rebooted.'),
        @('0x800B0109', 'The certificate is not trusted - run Install.bat as administrator again.'),
        @('0x800B010A', 'The certificate chain could not be built - the .cer file does not match this .appx.'),
        @('0x80073D02', 'The package is in use. Close the game and try again.'),
        @('0x80073D06', 'A newer version of the package is already installed.')
    )

    foreach ($hint in $hints) {
        if ($Message -like "*$($hint[0])*") {
            Write-Host "  Hint: $($hint[1])" -ForegroundColor Yellow
        }
    }
}

$exitCode = 0

try {
    Write-Host 'Bad Piggies::Rebooted - UWP package installer'
    Write-Host "Folder: $here"
    Write-Host "PowerShell language mode: $($ExecutionContext.SessionState.LanguageMode)"

    $packages = @(Get-ChildItem -LiteralPath $here -Filter '*.appx' -File -ErrorAction SilentlyContinue | Sort-Object Name)
    if ($packages.Count -eq 0) {
        throw 'No .appx package found in this folder. Keep Install.bat, Install.ps1, the .appx, the .cer and the Dependencies folder together.'
    }
    if ($packages.Count -gt 1) {
        Write-Warn2 "More than one .appx found, using $($packages[0].Name)"
    }
    $package = $packages[0]

    $certificates = @(Get-ChildItem -LiteralPath $here -Filter '*.cer' -File -ErrorAction SilentlyContinue | Sort-Object Name)
    $identity = Get-PackageInfo -FileName $package.Name

    Write-Host "Package: $($package.Name)"
    if ($identity.Version -or $identity.Architecture) {
        Write-Host "  Name: $($identity.Name)  Version: $($identity.Version)  Architecture: $($identity.Architecture)"
    }

    $dependencyDir = Join-Path $here 'Dependencies'
    $dependencies = @()
    if (Test-Path -LiteralPath $dependencyDir) {
        $dependencies = @(Get-ChildItem -LiteralPath $dependencyDir -Filter '*.appx' -File -ErrorAction SilentlyContinue)
    }

    if ($certificates.Count -eq 0) {
        Write-Warn2 'No .cer file next to the package - assuming the certificate is already trusted.'
    } else {
        Write-Step 'Trusting the signing certificate'
        foreach ($certificate in $certificates) {
            & certutil.exe -addstore -f TrustedPeople $certificate.FullName 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "certutil failed with exit code $LASTEXITCODE for $($certificate.Name)"
            }
            Write-Ok "Certificate trusted: $($certificate.Name)"
        }
    }

    if ($dependencies.Count -gt 0) {
        Write-Step 'Installing dependencies'
        foreach ($dependency in $dependencies) {
            try {
                Add-AppxPackage -Path $dependency.FullName -ErrorAction Stop
                Write-Ok "Dependency installed: $($dependency.Name)"
            } catch {
                # Already installed / newer version present is fine.
                $message = $_.Exception.Message
                if ($message -like '*0x80073CFB*' -or $message -like '*0x80073D06*' -or
                    $message -like '*already installed*') {
                    Write-Ok "Dependency already present: $($dependency.Name)"
                } else {
                    Write-Warn2 "Could not install $($dependency.Name): $message"
                }
            }
        }
    } else {
        Write-Warn2 'No Dependencies folder found - the game needs Microsoft.VCLibs.140.00 to be installed already.'
    }

    Write-Step 'Installing the game'
    $arguments = @{ Path = $package.FullName; ErrorAction = 'Stop' }
    if ($dependencies.Count -gt 0) {
        $arguments['DependencyPath'] = @($dependencies | ForEach-Object { $_.FullName })
    }

    try {
        Add-AppxPackage @arguments
        Write-Ok "Installed: $($identity.Name) $($identity.Version)"
    } catch {
        $message = $_.Exception.Message
        if ($message -like '*0x80073CFB*' -or $message -like '*0x80073D06*') {
            Write-Ok 'This version is already installed - nothing to do.'
        } else {
            Write-Err $message
            Show-HResultHint -Message $message
            throw
        }
    }

    $family = $null
    $installed = Get-AppxPackage -Name $identity.Name -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($installed -and $installed.PackageFamilyName) {
        $family = $installed.PackageFamilyName
    }

    if ($family -and -not $NoLaunchPrompt) {
        Write-Step 'Done'
        $answer = Read-Host 'Start Bad Piggies::Rebooted now? [Y/n]'
        if ($answer -eq '' -or $answer -match '^(y|Y|yes|YES)$') {
            Start-Process "shell:AppsFolder\$family!App"
        } else {
            Write-Host 'You can start the game from the Start menu.'
        }
    } else {
        Write-Step 'Done'
        Write-Host 'You can start the game from the Start menu.'
    }
} catch {
    Write-Err $_.Exception.Message
    Write-Host ''
    Write-Host 'Installation failed. Read the message above, then see README.txt in this folder.' -ForegroundColor Yellow
    $exitCode = 1
}

if (-not $NoPause) {
    Write-Host ''
    Read-Host 'Press Enter to close this window'
}

exit $exitCode
