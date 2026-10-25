param(
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

# 32-bit standalone player. This is the only build that runs on 32-bit Windows and it uses the
# same 32-bit code path (IntPtr.Size == 4 and the ARM32 splash) as the UWP ARM32 package, so it
# doubles as a local test target for the ARM builds.
try {
    Write-Step 'Building Windows x86 (Standalone / Mono)'
    Assert-BuildPrerequisites -Unity -UnityPath $UnityPath

    $output = Join-Path $BuildsRoot 'Win32'
    Reset-OutputFolder -Path $output -Label 'Win32'
    Invoke-UnityBuild -Method 'BpReBuild.BuildWin32' -LogName 'win32' -UnityPath $UnityPath

    Assert-Path -Path (Join-Path $output 'BadPiggiesRebooted.exe') -Label 'Win32 player'
    Assert-Path -Path (Join-Path $output 'BadPiggiesRebooted_Data\Managed\Assembly-CSharp.dll') -Label 'Win32 managed assembly'

    Write-Ok "Win32 build: $(Join-Path $output 'BadPiggiesRebooted.exe')"

    if (-not $NoPublish) {
        Write-Step 'Publishing Windows x86'
        Publish-Standalone -Target Win32
    }
} catch {
    if (-not $NoPause) {
        Write-Failure -Message $_.Exception.Message
        Wait-ForKeyPress
    }
    throw
}
