param(
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

$unityOutput = Join-Path $BuildsRoot 'WSA86\Bad PiggiesRebooted'
$solution    = Join-Path $BuildsRoot 'WSA86\Bad PiggiesRebooted.sln'
$srcData     = Join-Path $unityOutput 'Data'
# The Unity UWP solution calls the 32-bit project platform "Win32" even though the solution
# configuration in front of it is "x86".
$looseOutput = Join-Path $BuildsRoot 'WSA86\build\bin\Win32\Release'

try {
    Write-Step 'Building UWP x86 (IL2CPP)'
    Assert-BuildPrerequisites -Unity -Uwp -UnityPath $UnityPath -MSBuildPath $MSBuildPath

    Invoke-UnityBuild -Method 'BpReBuild.BuildWSA86' -LogName 'uwp-x86' -UnityPath $UnityPath

    Write-Host "  Patching UWP splash and icon (Unity output)..."
    Set-UwpSplash -AssetsDir (Join-Path $unityOutput 'Assets') -ManifestPath (Join-Path $unityOutput 'Package.appxmanifest')
    Set-UwpIcons -UnityOutput $unityOutput
    Set-UwpCertificate -UnityOutput $unityOutput -ManifestPath (Join-Path $unityOutput 'Package.appxmanifest')
    Set-UwpProjectSettings -UnityOutput $unityOutput

    Invoke-MSBuild -Solution $solution -Platform 'x86' -MSBuildPath $MSBuildPath

    # MSBuild/Unity call the 32-bit project platform "Win32" - rename the package to _x86.
    Rename-UwpX86Package -PackagesRoot (Join-Path $BuildsRoot 'WSA86\AppPackages')

    Sync-DataFolder -Source $srcData -Destination (Join-Path $looseOutput 'Data')

    Write-Host "  Patching UWP splash and icon (loose output)..."
    Set-UwpSplash -AssetsDir (Join-Path $looseOutput 'Assets') -ManifestPath (Join-Path $looseOutput 'AppxManifest.xml')
    Set-UwpIcons -UnityOutput $looseOutput
    Set-UwpCertificate -UnityOutput $looseOutput -ManifestPath (Join-Path $looseOutput 'AppxManifest.xml')

    Assert-Path -Path (Join-Path $looseOutput 'GameAssembly.dll') -Label 'x86 GameAssembly.dll'
    Assert-Path -Path (Join-Path $looseOutput 'Data\il2cpp_data\Metadata\global-metadata.dat') -Label 'x86 global-metadata.dat'

    $appx = Get-LatestAppPackage -Root (Join-Path $BuildsRoot 'WSA86\AppPackages') -Architecture 'x86'
    if (-not $appx) {
        $appx = Get-LatestAppPackage -Root (Join-Path $BuildsRoot 'WSA86\AppPackages') -Architecture 'Win32'
    }
    if ($appx) {
        Write-Ok "UWP x86 build: $($appx.FullName)"
    } else {
        Write-Warn "x86 player built, but no *_x86_Test package was found under Builds\WSA86\AppPackages"
    }

    Write-Ok "UWP x86 output: $looseOutput"

    if (-not $NoPublish) {
        Write-Step 'Publishing UWP x86'
        Publish-UwpAppx -Architecture x86
    }
} catch {
    if (-not $NoPause) {
        Write-Failure -Message $_.Exception.Message
        Wait-ForKeyPress
    }
    throw
}
