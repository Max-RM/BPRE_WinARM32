# Build-Common.ps1
#
# Shared helpers for the Bad Piggies :: Rebooted build scripts.
# Dot-source this file from the other scripts:
#     . (Join-Path $PSScriptRoot "Build-Common.ps1")

$ErrorActionPreference = 'Stop'

$Script:ProjectRoot   = Split-Path -Parent $PSScriptRoot
$Script:BuildsRoot    = Join-Path $ProjectRoot 'Builds'
$Script:PublishRoot   = Join-Path $ProjectRoot 'Publish'
$Script:BuildLogDir   = Join-Path $env:TEMP 'bpre-builds'
$Script:PlayerDataCache = Join-Path $ProjectRoot 'Library\PlayerDataCache'
$Script:UwpSplashPng  = Join-Path $ProjectRoot 'BuildSupport\UWP\SplashScreen.scale-200.png'
$Script:UwpInstallDir = Join-Path $ProjectRoot 'BuildSupport\UWP\Install'
$Script:UwpSplashBg   = '#FFF6DD'
$Script:WinSdkVersion = '10.0.19041.0'
$Script:PreferredVcTools = '14.38.33130'

# Required tooling. The links are shown by the prerequisite checks when something is missing.
$Script:RequiredUnityVersion = '2018.4.36f1'
$Script:UnityDownloadUrl     = 'https://download.unity3d.com/download_unity/6cd387d23174/Windows64EditorInstaller/UnitySetup64-2018.4.36f1.exe'
$Script:UnityArchiveUrl      = 'https://unity.com/releases/editor/archive'
$Script:UnityBackupUrl       = 'https://t.me/TNT_ENTERTAINMENT_inc/165114'
$Script:VsDownloadsUrl       = 'https://visualstudio.microsoft.com/downloads/'
$Script:UwpWorkloadId        = 'Microsoft.VisualStudio.Workload.Universal'
$Script:ArmToolsId           = 'Microsoft.VisualStudio.Component.VC.Tools.ARM'
$Script:WinSdkComponentId    = 'Microsoft.VisualStudio.Component.Windows10SDK.19041'

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "=== $Message ===" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)
    Write-Host "  OK: $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "  WARNING: $Message" -ForegroundColor Yellow
}

function Assert-Path {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label not found: $Path"
    }
}

function Show-Dialog {
    # Small modal dialog so a double-clicked script can explain what is missing.
    # Returns $true for OK/Yes, $false for No.
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [string]$Title = 'Bad Piggies :: Rebooted - build',
        [ValidateSet('OK', 'YesNo')][string]$Buttons = 'OK',
        [ValidateSet('Info', 'Warning', 'Error', 'Question')][string]$Icon = 'Info'
    )

    $flags = switch ($Icon) {
        'Error'    { 0x10 }
        'Question' { 0x20 }
        'Warning'  { 0x30 }
        default    { 0x40 }
    }
    if ($Buttons -eq 'YesNo') { $flags = $flags -bor 0x04 }

    try {
        $shell = New-Object -ComObject WScript.Shell
        $answer = $shell.Popup($Text, 0, $Title, $flags)
        if ($Buttons -eq 'YesNo') { return ($answer -eq 6) }
        return $true
    } catch {
        Write-Host ''
        Write-Host $Text -ForegroundColor Yellow
        if ($Buttons -eq 'YesNo') {
            $answer = Read-Host 'Continue? [y/N]'
            return ($answer -match '^(?i)\s*y')
        }
        return $true
    }
}

function Wait-ForKeyPress {
    param([string]$Message = 'Press any key to exit...')

    Write-Host ''
    Write-Host $Message -ForegroundColor Yellow
    try {
        if ([Console]::IsInputRedirected) { return }
        [void][Console]::ReadKey($true)
    } catch {
        Start-Sleep -Seconds 5
    }
}

function Write-Failure {
    # Show the error in a dialog (a script started from Explorer has no console output left)
    # and in the console. The caller decides whether to pause afterwards.
    param([Parameter(Mandatory = $true)][string]$Message)

    Write-Host ''
    Write-Host 'BUILD FAILED' -ForegroundColor Red
    Write-Host "  $Message" -ForegroundColor Red
    Show-Dialog -Icon Error -Title 'Bad Piggies :: Rebooted - build failed' `
        -Text ("The build could not be completed.`n`n$Message`n`nFix the problem, then run the script again.") | Out-Null
}

function Get-VsWherePath {
    $candidate = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $candidate) { return $candidate }
    return $null
}

function Get-VsInstallPath {
    $vswhere = Get-VsWherePath
    if (-not $vswhere) { return $null }

    $path = & $vswhere -latest -products * -property installationPath 2>$null | Select-Object -First 1
    if ($path -and (Test-Path -LiteralPath $path)) { return $path }
    return $null
}

function Test-VsComponent {
    # True when the newest Visual Studio that provides the component is installed.
    param([Parameter(Mandatory = $true)][string]$ComponentId)

    $vswhere = Get-VsWherePath
    if (-not $vswhere) { return $false }

    $path = & $vswhere -latest -products * -requires $ComponentId -property installationPath 2>$null |
        Select-Object -First 1
    return [bool]$path
}

function Install-VsComponent {
    # Offers to add a component through the Visual Studio Installer. Returns $true when the
    # component is present afterwards. Requires elevation, so the installer is started with RunAs.
    param(
        [Parameter(Mandatory = $true)][string]$ComponentId,
        [Parameter(Mandatory = $true)][string]$DisplayName
    )

    if (Test-VsComponent -ComponentId $ComponentId) { return $true }

    $vsPath = Get-VsInstallPath
    if (-not $vsPath) { return $false }

    $running = @(Get-Process -Name 'devenv', 'MSBuild', 'VSIXInstaller', 'ServiceHub.Host.CLR.x64' -ErrorAction SilentlyContinue)
    if ($running.Count -gt 0) {
        Show-Dialog -Icon Warning -Title 'Close Visual Studio first' -Text (
            "Visual Studio (or MSBuild) is running. Close it, then start the build again so that " +
            "'$DisplayName' can be installed.") | Out-Null
        return $false
    }

    $answer = Show-Dialog -Buttons YesNo -Icon Question -Title 'Missing build component' -Text (
        "The build needs this Visual Studio component:`n`n  $DisplayName`n`n" +
        "It is not installed. Download and install it now with the Visual Studio Installer?`n" +
        "(internet connection and administrator rights are required)")
    if (-not $answer) { return $false }

    $setup = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\setup.exe'
    if (-not (Test-Path -LiteralPath $setup)) {
        Show-Dialog -Icon Warning -Title 'Visual Studio Installer not found' -Text (
            "The Visual Studio Installer was not found at:`n  $setup`n`n" +
            "Install '$DisplayName' manually from the Visual Studio Installer, or download " +
            "Visual Studio from $VsDownloadsUrl") | Out-Null
        return $false
    }

    # setup.exe needs the path in quotes and cancels silently when a VS process is running.
    $arguments = @('modify', '--installPath', ('"' + $vsPath + '"'), '--add', $ComponentId, '--quiet', '--norestart')
    Write-Host "  Installing: $DisplayName"
    try {
        Start-Process -FilePath $setup -ArgumentList $arguments -Verb RunAs -Wait | Out-Null
    } catch {
        Show-Dialog -Icon Error -Title 'Visual Studio Installer' -Text (
            "Could not start the Visual Studio Installer:`n`n$($_.Exception.Message)") | Out-Null
        return $false
    }

    # The installer may return before all of its worker processes have finished.
    $deadline = (Get-Date).AddMinutes(30)
    while ((Get-Date) -lt $deadline -and
           (Get-Process -Name 'setup', 'vs_installer', 'vs_installershell', 'vs_bootstrapper' -ErrorAction SilentlyContinue)) {
        Start-Sleep -Seconds 5
    }

    if (Test-VsComponent -ComponentId $ComponentId) {
        Write-Ok "$DisplayName installed"
        return $true
    }
    return $false
}

function Assert-BuildPrerequisites {
    # Checks Unity, Visual Studio, the UWP workload, the Windows SDK and (optionally) the ARM
    # toolset before a build starts. Missing components that can be installed quietly are
    # offered through a dialog; everything else ends in a message that explains what to install.
    param(
        [switch]$Unity,
        [switch]$Uwp,
        [switch]$Arm,
        [string]$UnityPath,
        [string]$MSBuildPath
    )

    if ($Unity) {
        # Do not use $unity here: PowerShell variable names are case-insensitive, so it would
        # overwrite the [switch]$Unity parameter (and its SwitchParameter type constraint).
        $unityExe = $null
        try { $unityExe = Get-UnityPath -Explicit $UnityPath } catch { }

        if (-not $unityExe) {
            $open = Show-Dialog -Buttons YesNo -Icon Warning -Title 'Unity is not installed' -Text (
                "Unity $RequiredUnityVersion was not found.`n`n" +
                "Download page:  $UnityArchiveUrl`n" +
                "Direct installer:  $UnityDownloadUrl`n" +
                "Mirror:  $UnityBackupUrl`n`n" +
                "Open the Unity download page in the browser now?")
            if ($open) { Start-Process $UnityArchiveUrl }
            throw "Unity $RequiredUnityVersion was not found. Install it (see the links above) or pass -UnityPath."
        }

        $installedVersion = (Get-Item -LiteralPath $unityExe).VersionInfo.ProductVersion
        if ($installedVersion -match '^(\d+\.\d+\.\d+[a-z]\d+)') { $installedVersion = $Matches[1] }

        $projectVersionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
        $projectVersion = $RequiredUnityVersion
        if (Test-Path -LiteralPath $projectVersionFile) {
            $match = Select-String -LiteralPath $projectVersionFile -Pattern 'm_EditorVersion:\s*(\S+)' |
                Select-Object -First 1
            if ($match) { $projectVersion = $match.Matches[0].Groups[1].Value }
        }

        if ($installedVersion -and $projectVersion -and $installedVersion -ne $projectVersion) {
            $continue = Show-Dialog -Buttons YesNo -Icon Warning -Title 'Unity version mismatch' -Text (
                "The project was created with Unity $projectVersion, but the found editor is $installedVersion.`n`n" +
                "A newer editor will upgrade the project files and cannot be undone.`n`n" +
                "Continue anyway?")
            if (-not $continue) {
                throw "Unity version mismatch: project $projectVersion, editor $installedVersion."
            }
        }

        if ($Uwp) {
            $editorDir = Split-Path -Parent $unityExe
            $metro = Join-Path $editorDir 'Data\PlaybackEngines\MetroSupport'
            if (-not (Test-Path -LiteralPath $metro)) {
                throw ("This Unity installation has no UWP build support (MetroSupport). `n" +
                    "Add 'Universal Windows Platform Build Support' for Unity $RequiredUnityVersion in the Unity Hub, then run again.")
            }
        }
    }

    if ($Uwp) {
        $msbuild = $null
        try { $msbuild = Get-MSBuildPath -Explicit $MSBuildPath } catch { }

        if (-not $msbuild) {
            $open = Show-Dialog -Buttons YesNo -Icon Warning -Title 'Visual Studio is not installed' -Text (
                "MSBuild was not found. The UWP builds need Visual Studio 2022 with the `n" +
                "'Universal Windows Platform development' workload.`n`n" +
                "Download: $VsDownloadsUrl`n`n" +
                "Open the Visual Studio download page now?")
            if ($open) { Start-Process $VsDownloadsUrl }
            throw "MSBuild was not found. Install Visual Studio 2022 with the UWP workload (see the link above)."
        }

        if (-not (Test-VsComponent -ComponentId $UwpWorkloadId)) {
            $installed = Install-VsComponent -ComponentId $UwpWorkloadId `
                -DisplayName 'Universal Windows Platform development'
            if (-not $installed) {
                throw ("The 'Universal Windows Platform development' workload is missing.`n" +
                    "Open the Visual Studio Installer, choose Modify, enable the workload`n" +
                    "(and 'MSVC v143 - VS 2022 C++ ARM build tools' for the ARM32 build),`n" +
                    "or install Visual Studio from $VsDownloadsUrl")
            }
        }

        $sdkInclude = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\Include\$WinSdkVersion\um\windows.h"
        if (-not (Test-Path -LiteralPath $sdkInclude)) {
            $installed = Install-VsComponent -ComponentId $WinSdkComponentId `
                -DisplayName "Windows 10 SDK $WinSdkVersion"
            $sdkInclude = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\Include\$WinSdkVersion\um\windows.h"
            if (-not (Test-Path -LiteralPath $sdkInclude)) {
                throw ("Windows 10 SDK $WinSdkVersion was not found.`n" +
                    "Install it from the Visual Studio Installer (Individual components ->`n" +
                    "'Windows 10 SDK ($WinSdkVersion)') and run the script again.")
            }
        }
    }

    if ($Arm) {
        # Assert-ArmToolset offers to install the ARM build tools when they are missing.
        Assert-ArmToolset
    }
}

function Get-UnityPath {
    param([string]$Explicit)

    foreach ($candidate in @($Explicit, $env:UNITY_EDITOR)) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $fixed = @(
        'C:\Program Files\Unity\Editor\Unity.exe',
        'C:\Program Files\Unity\Hub\Editor\2018.4.36f1\Editor\Unity.exe'
    )
    foreach ($candidate in $fixed) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }

    $hubRoot = 'C:\Program Files\Unity\Hub\Editor'
    if (Test-Path -LiteralPath $hubRoot) {
        $found = Get-ChildItem -LiteralPath $hubRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1
        if ($found) { return $found }
    }

    throw "Unity.exe was not found. Set UNITY_EDITOR or pass -UnityPath."
}

function Get-MSBuildPath {
    param([string]$Explicit)

    foreach ($candidate in @($Explicit, $env:MSBUILD_EXE)) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
            -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1
        if ($found -and (Test-Path -LiteralPath $found)) { return $found }
    }

    $fixed = @(
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe'
    )
    foreach ($candidate in $fixed) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }

    throw "MSBuild.exe was not found. Install Visual Studio with the UWP workload or set MSBUILD_EXE."
}

function Get-VCToolsVersion {
    param([switch]$RequireArm)

    # The UWP projects in this repository are known to build with 14.38. If it is not installed
    # (or it has no ARM32 compiler when one is required) fall back to the default toolset by
    # returning $null, which means "do not pass /p:VCToolsVersion".
    $msvcRoot = Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio\2022'
    foreach ($edition in @('Community', 'Professional', 'Enterprise', 'BuildTools')) {
        $tools = Join-Path $msvcRoot "$edition\VC\Tools\MSVC"
        if (-not (Test-Path -LiteralPath $tools)) { continue }

        if (-not $RequireArm) {
            if (Test-Path -LiteralPath (Join-Path $tools $PreferredVcTools)) { return $PreferredVcTools }
            continue
        }

        # ARM32 support is an optional component; only pick a toolset that actually has it.
        $versions = @(Get-ChildItem -LiteralPath $tools -Directory | Sort-Object Name -Descending)
        $ordered  = @($versions | Where-Object { $_.Name -eq $PreferredVcTools }) +
                    @($versions | Where-Object { $_.Name -ne $PreferredVcTools })
        foreach ($version in $ordered) {
            if (Test-Path -LiteralPath (Join-Path $version.FullName 'bin\Hostx64\arm\cl.exe')) {
                return $version.Name
            }
        }
    }
    return $null
}

function Get-DefaultVcToolsetPath {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) { return $null }

    $vsPath = & $vswhere -latest -products * -property installationPath 2>$null | Select-Object -First 1
    if (-not $vsPath) { return $null }

    $defaultFile = Join-Path $vsPath 'VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt'
    if (-not (Test-Path -LiteralPath $defaultFile)) { return $null }

    $version = ([System.IO.File]::ReadAllText($defaultFile)).Trim()
    if (-not $version) { return $null }
    return (Join-Path $vsPath "VC\Tools\MSVC\$version")
}

function Assert-ArmToolset {
    # IL2CPP resolves MSVC from the Visual Studio "default toolset" file, so that toolset
    # must ship the ARM (ARMv7) compiler.
    $toolset = Get-DefaultVcToolsetPath
    if ($toolset) {
        $armCl = Join-Path $toolset 'bin\Hostx64\arm\cl.exe'
        if (Test-Path -LiteralPath $armCl) { return }
    }

    # The ARM build tools are an optional component, so offer to add them first.
    if (-not (Test-VsComponent -ComponentId $ArmToolsId)) {
        $null = Install-VsComponent -ComponentId $ArmToolsId `
            -DisplayName 'MSVC v143 - VS 2022 C++ ARM build tools'
    }

    $toolset = Get-DefaultVcToolsetPath
    if ($toolset) {
        $armCl = Join-Path $toolset 'bin\Hostx64\arm\cl.exe'
        if (Test-Path -LiteralPath $armCl) {
            Write-Ok "ARM32 toolset: $(Split-Path -Leaf $toolset)"
            return
        }
    }

    if (-not $toolset) {
        throw ("No Visual Studio MSVC toolset was found (Microsoft.VCToolsVersion.default.txt is missing).`n" +
            "Install Visual Studio 2022 with the UWP workload and the ARM build tools.")
    }

    $message = @"
The default MSVC toolset has no ARM32 compiler: $toolset
  IL2CPP needs the "MSVC v143 - VS 2022 C++ ARM build tools" component
  (Microsoft.VisualStudio.Component.VC.Tools.ARM) installed in Visual Studio.
"@

    $alternative = Get-VCToolsVersion -RequireArm
    if ($alternative) {
        $message += @"

  Toolset $alternative does have ARM32 support, but IL2CPP reads the default toolset file, not
  this script. Either install the ARM build tools for the default toolset, or point
  VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt at $alternative (requires admin rights).
"@
    } else {
        $message += @"

  Install it from the Visual Studio Installer (Individual components -> Compilers, build tools
  and runtimes) or run:
    & "`${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\setup.exe" modify `
        --installPath "<VS install path>" --add Microsoft.VisualStudio.Component.VC.Tools.ARM `
        --quiet --norestart
"@
    }

    throw $message
}

function Invoke-UnityBuild {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$LogName,
        [string]$UnityPath
    )

    if (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue) {
        throw "Unity.exe is already running. Close the Unity editor before running a batch build."
    }

    $unity = Get-UnityPath -Explicit $UnityPath
    New-Item -ItemType Directory -Force -Path $BuildLogDir | Out-Null
    $log = Join-Path $BuildLogDir ("{0}-{1}.log" -f $LogName, (Get-Date -Format 'yyyyMMdd-HHmmss'))

    Write-Host "  Unity:   $unity"
    Write-Host "  Method:  $Method"
    Write-Host "  Log:     $log"

    # Unity 2018 keeps serialized player data in Library\PlayerDataCache and reuses it when the
    # build target changes. The reused copy is not always valid for the new target: a 32-bit
    # standalone built after an x64 or UWP build then dies at startup with
    # "resources.assets is corrupted! [Position out of bounds!]". Dropping the cache forces a
    # clean serialization for this target (costs a few seconds, not a full reimport).
    if (Test-Path -LiteralPath $PlayerDataCache) {
        Remove-Item -LiteralPath $PlayerDataCache -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Player data cache cleared"
    }

    # Start-Process -Wait also waits for every descendant process. Unity leaves the Roslyn
    # compiler server (VBCSCompiler.exe) running, so -Wait can block long after Unity exited.
    # Waiting on the process object itself returns as soon as Unity terminates.
    $proc = Start-Process -FilePath $unity -PassThru -ArgumentList @(
        '-batchmode', '-quit',
        '-projectPath', ('"' + $ProjectRoot + '"'),
        '-executeMethod', $Method,
        '-logFile', ('"' + $log + '"')
    )
    $proc.WaitForExit()

    $exitCode = $proc.ExitCode
    if ($exitCode -ne 0) {
        Write-Host "  Last log lines:" -ForegroundColor Red
        Get-Content -LiteralPath $log -Tail 40 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    $_" }
        throw "Unity build failed with exit code $exitCode. See $log"
    }
    Write-Ok "Unity build finished"
}

function Reset-OutputFolder {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    # Unity 2018 appends player data into an existing build folder, which can leave
    # resources.assets corrupted ("Position out of bounds!" + crash on startup) when the
    # folder was produced by an earlier build. Every build therefore starts from scratch.
    if (Test-Path -LiteralPath $Path) {
        Write-Host "  Removing previous $Label output: $Path"
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-MSBuild {
    param(
        [Parameter(Mandatory = $true)][string]$Solution,
        [Parameter(Mandatory = $true)][ValidateSet('x64', 'ARM', 'ARM64', 'x86')][string]$Platform,
        [string]$Configuration = 'Release',
        [string]$MSBuildPath
    )

    $msbuild = Get-MSBuildPath -Explicit $MSBuildPath
    $vcTools = Get-VCToolsVersion -RequireArm:($Platform -eq 'ARM')

    $arguments = @(
        $Solution,
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:WindowsTargetPlatformVersion=$WinSdkVersion"
    )
    if ($vcTools) { $arguments += "/p:VCToolsVersion=$vcTools" }
    $arguments += @('/m', '/v:minimal', '/nologo')

    Write-Host "  MSBuild: $msbuild"
    Write-Host "  Target:  $Platform / $Configuration"
    if ($vcTools) { Write-Host "  Toolset: $vcTools" }

    & $msbuild @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed for $Platform with exit code $LASTEXITCODE"
    }
    Write-Ok "MSBuild finished"
}

function Copy-Tree {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    Assert-Path -Path $Source -Label 'Source folder'
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    # /MIR keeps the destination an exact copy (stale files are removed).
    & robocopy $Source $Destination /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed with exit code $LASTEXITCODE ($Source -> $Destination)"
    }
}

function Sync-DataFolder {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    Assert-Path -Path $Source -Label 'Unity Data folder'

    # MSBuild does not copy the Unity Data folder next to the loose player,
    # so copy it explicitly (the metadata must match GameAssembly.dll).
    Copy-Tree -Source $Source -Destination $Destination
    Write-Ok "Data synced to $Destination"
}

function Set-UwpSplash {
    param(
        [Parameter(Mandatory = $true)][string]$AssetsDir,
        [Parameter(Mandatory = $true)][string]$ManifestPath
    )

    if (-not (Test-Path -LiteralPath $UwpSplashPng)) {
        Write-Warn "UWP splash image not found, skipping: $UwpSplashPng"
        return
    }

    if (Test-Path -LiteralPath $AssetsDir) {
        Copy-Item -LiteralPath $UwpSplashPng -Destination (Join-Path $AssetsDir 'SplashScreen.scale-200.png') -Force
        Write-Ok "UWP splash image replaced in $AssetsDir"
    }

    if (Test-Path -LiteralPath $ManifestPath) {
        $manifest = [System.IO.File]::ReadAllText($ManifestPath)
        $patched = [regex]::Replace(
            $manifest,
            '(<uap:SplashScreen\b[^>]*?BackgroundColor=")#[0-9A-Fa-f]{6}(")',
            ('${1}' + $UwpSplashBg + '${2}')
        )
        if ($patched -ne $manifest) {
            $utf8 = New-Object System.Text.UTF8Encoding($false)
            [System.IO.File]::WriteAllText((Resolve-Path -LiteralPath $ManifestPath), $patched, $utf8)
            Write-Ok "UWP splash background set to $UwpSplashBg"
        }
    }
}

function Get-LatestAppPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Architecture
    )

    if (-not (Test-Path -LiteralPath $Root)) { return $null }

    return Get-ChildItem -LiteralPath $Root -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*_${Architecture}_Test" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Publish-Standalone {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('Win32', 'Win64')][string]$Target,
        [string]$Destination
    )

    $source = Join-Path $BuildsRoot $Target
    if (-not $Destination) { $Destination = Join-Path $PublishRoot $Target }

    Copy-Tree -Source $source -Destination $Destination
    Write-Ok "$Target published: $Destination"
}

function Get-VcLibsAppxPath {
    # Microsoft.VCLibs.140.00 is a hard dependency of the generated appx manifest.
    param([Parameter(Mandatory = $true)][ValidateSet('x64', 'ARM')][string]$Architecture)

    $roots = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Windows Kits\10\ExtensionSDKs\Microsoft.VCLibs\14.0\Appx\Retail'),
        (Join-Path $env:ProgramFiles 'Microsoft SDKs\Windows Kits\10\ExtensionSDKs\Microsoft.VCLibs\14.0\Appx\Retail')
    )

    $fileName = if ($Architecture -eq 'ARM') {
        'Microsoft.VCLibs.arm.14.00.appx'
    } else {
        'Microsoft.VCLibs.x64.14.00.appx'
    }

    foreach ($root in $roots) {
        $path = Join-Path (Join-Path $root $Architecture) $fileName
        if (Test-Path -LiteralPath $path) { return $path }
    }
    return $null
}

function Publish-UwpAppx {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('x64', 'ARM')][string]$Architecture,
        [string]$Destination
    )

    if ($Architecture -eq 'ARM') {
        $packagesRoot = Join-Path $BuildsRoot 'WSA\AppPackages'
        $folderName   = 'UWP-ARM32'
    } else {
        $packagesRoot = Join-Path $BuildsRoot 'WSA64\AppPackages'
        $folderName   = 'UWP-x64'
    }
    if (-not $Destination) { $Destination = Join-Path $PublishRoot $folderName }

    $package = Get-LatestAppPackage -Root $packagesRoot -Architecture $Architecture
    if (-not $package) {
        Write-Warn "no *_${Architecture}_Test package under $packagesRoot, nothing published"
        return
    }

    $files = Get-ChildItem -LiteralPath $package.FullName -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.appx', '.cer') }
    if (-not $files) {
        throw "No .appx/.cer files in $($package.FullName)"
    }

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    foreach ($file in $files) {
        Copy-Item -LiteralPath $file.FullName -Destination $Destination -Force
    }

    # Installer (Install.bat / Install.ps1) and the end-user instructions.
    $published = @($files)
    foreach ($name in @('Install.bat', 'Install.ps1', 'README.txt')) {
        $source = Join-Path $UwpInstallDir $name
        if (-not (Test-Path -LiteralPath $source)) {
            Write-Warn "installer file not found, skipped: $source"
            continue
        }
        Copy-Item -LiteralPath $source -Destination $Destination -Force
        $published += Get-Item -LiteralPath (Join-Path $Destination $name)
    }

    # Dependency package: without it a clean device rejects the game with 0x80073CF3.
    $dependenciesDir = Join-Path $Destination 'Dependencies'
    New-Item -ItemType Directory -Force -Path $dependenciesDir | Out-Null
    $vclibs = Get-VcLibsAppxPath -Architecture $Architecture
    if ($vclibs) {
        Copy-Item -LiteralPath $vclibs -Destination $dependenciesDir -Force
        $published += Get-Item -LiteralPath (Join-Path $dependenciesDir (Split-Path -Leaf $vclibs))
    } else {
        Write-Warn "Microsoft.VCLibs.$Architecture.14.00.appx was not found in the Windows SDK"
        Write-Warn "  install the Windows 10 SDK $WinSdkVersion, or put the dependency into Dependencies manually"
    }

    Write-Ok "UWP $Architecture package published: $Destination"
    foreach ($file in $published) {
        Write-Host "    $($file.Name)"
    }
}
