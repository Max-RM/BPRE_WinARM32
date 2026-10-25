param(
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

$unityOutput = Join-Path $BuildsRoot 'WSA64\Bad PiggiesRebooted'
$solution    = Join-Path $BuildsRoot 'WSA64\Bad PiggiesRebooted.sln'
$srcData     = Join-Path $unityOutput 'Data'
$looseOutput = Join-Path $BuildsRoot 'WSA64\build\bin\x64\Release'

try {
    Write-Step 'Building UWP x64 (IL2CPP)'
    Assert-BuildPrerequisites -Unity -Uwp -UnityPath $UnityPath -MSBuildPath $MSBuildPath

    Invoke-UnityBuild -Method 'BpReBuild.BuildWSA64' -LogName 'uwp-x64' -UnityPath $UnityPath

    Write-Host "  Patching UWP splash (Unity output)..."
    Set-UwpSplash -AssetsDir (Join-Path $unityOutput 'Assets') -ManifestPath (Join-Path $unityOutput 'Package.appxmanifest')

    Invoke-MSBuild -Solution $solution -Platform 'x64' -MSBuildPath $MSBuildPath

    Sync-DataFolder -Source $srcData -Destination (Join-Path $looseOutput 'Data')

    Write-Host "  Patching UWP splash (loose output)..."
    Set-UwpSplash -AssetsDir (Join-Path $looseOutput 'Assets') -ManifestPath (Join-Path $looseOutput 'AppxManifest.xml')

    Assert-Path -Path (Join-Path $looseOutput 'GameAssembly.dll') -Label 'x64 GameAssembly.dll'
    Assert-Path -Path (Join-Path $looseOutput 'Data\il2cpp_data\Metadata\global-metadata.dat') -Label 'x64 global-metadata.dat'

    Write-Ok "UWP x64 loose build: $looseOutput"

    if (-not $NoPublish) {
        Write-Step 'Publishing UWP x64'
        Publish-UwpAppx -Architecture x64
    }
} catch {
    if (-not $NoPause) {
        Write-Failure -Message $_.Exception.Message
        Wait-ForKeyPress
    }
    throw
}
