param(
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

try {
    Write-Step 'Building Windows x64 (Standalone / Mono)'
    Assert-BuildPrerequisites -Unity -UnityPath $UnityPath

    $output = Join-Path $BuildsRoot 'Win64'
    Invoke-StandaloneBuild -Target Win64 -Method 'BpReBuild.BuildWin64' -LogName 'win64' -UnityPath $UnityPath

    Assert-Path -Path (Join-Path $output 'BadPiggiesRebooted.exe') -Label 'Win64 player'
    Assert-Path -Path (Join-Path $output 'BadPiggiesRebooted_Data\Managed\Assembly-CSharp.dll') -Label 'Win64 managed assembly'

    Write-Ok "Win64 build: $(Join-Path $output 'BadPiggiesRebooted.exe')"

    if (-not $NoPublish) {
        Write-Step 'Publishing Windows x64'
        Publish-Standalone -Target Win64
    }
} catch {
    if (-not $NoPause) {
        Write-Failure -Message $_.Exception.Message
        Wait-ForKeyPress
    }
    throw
}
