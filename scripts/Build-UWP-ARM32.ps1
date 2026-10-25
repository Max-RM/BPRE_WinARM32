param(
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

$unityOutput = Join-Path $BuildsRoot 'WSA\Bad PiggiesRebooted'
$solution    = Join-Path $BuildsRoot 'WSA\Bad PiggiesRebooted.sln'
$srcData     = Join-Path $unityOutput 'Data'
$armOutput   = Join-Path $BuildsRoot 'WSA\build\bin\ARM\Release'

try {
    Write-Step 'Building UWP ARM32 (IL2CPP)'
    Assert-BuildPrerequisites -Unity -Uwp -Arm -UnityPath $UnityPath -MSBuildPath $MSBuildPath

    Invoke-UnityBuild -Method 'BpReBuild.BuildWSA' -LogName 'uwp-arm32' -UnityPath $UnityPath

    Write-Host "  Patching UWP splash (Unity output)..."
    Set-UwpSplash -AssetsDir (Join-Path $unityOutput 'Assets') -ManifestPath (Join-Path $unityOutput 'Package.appxmanifest')

    Invoke-MSBuild -Solution $solution -Platform 'ARM' -MSBuildPath $MSBuildPath

    Sync-DataFolder -Source $srcData -Destination (Join-Path $armOutput 'Data')

    Write-Host "  Patching UWP splash (loose output)..."
    Set-UwpSplash -AssetsDir (Join-Path $armOutput 'Assets') -ManifestPath (Join-Path $armOutput 'AppxManifest.xml')

    Assert-Path -Path (Join-Path $armOutput 'GameAssembly.dll') -Label 'ARM GameAssembly.dll'
    Assert-Path -Path (Join-Path $armOutput 'Data\il2cpp_data\Metadata\global-metadata.dat') -Label 'ARM global-metadata.dat'

    $appx = Get-LatestAppPackage -Root (Join-Path $BuildsRoot 'WSA\AppPackages') -Architecture 'ARM'
    if ($appx) {
        Write-Ok "ARM32 build: $($appx.FullName)"
    } else {
        Write-Warn "ARM32 player built, but no *_ARM_Test package was found under Builds\WSA\AppPackages"
    }

    Write-Ok "UWP ARM32 output: $armOutput"

    if (-not $NoPublish) {
        Write-Step 'Publishing UWP ARM32'
        Publish-UwpAppx -Architecture ARM
    }
} catch {
    if (-not $NoPause) {
        Write-Failure -Message $_.Exception.Message
        Wait-ForKeyPress
    }
    throw
}
