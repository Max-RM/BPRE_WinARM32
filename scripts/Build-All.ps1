param(
    [ValidateSet('win64', 'win32', 'uwp-x64', 'uwp-arm32')]
    [string[]]$Targets = @('uwp-arm32', 'uwp-x64', 'win32', 'win64'),
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

$scripts = @{
    'win64'      = 'Build-Win64.ps1'
    'win32'      = 'Build-Win32.ps1'
    'uwp-x64'    = 'Build-UwpX64.ps1'
    'uwp-arm32'  = 'Build-UwpArm32.ps1'
}

Write-Step "Building: $($Targets -join ', ')"
$started = Get-Date
$failed = @()

foreach ($target in $Targets) {
    $script = Join-Path $PSScriptRoot $scripts[$target]
    Write-Step "Target: $target"
    try {
        # -NoPause: the child scripts must not stop for a key press, Build-All does that once at the end.
        & $script -UnityPath $UnityPath -MSBuildPath $MSBuildPath -NoPublish:$NoPublish -NoPause
    } catch {
        Write-Host "  FAILED: $target - $($_.Exception.Message)" -ForegroundColor Red
        $failed += $target
    }
}

$elapsed = (Get-Date) - $started
Write-Step ("Done in {0:hh\:mm\:ss}" -f $elapsed)
if ($failed.Count -gt 0) {
    Write-Host "  Failed targets: $($failed -join ', ')" -ForegroundColor Red
    Show-Dialog -Icon Error -Title 'Bad Piggies :: Rebooted - build failed' `
        -Text ("These targets failed: $($failed -join ', ')`n`n" +
               "See the console output above, fix the problem and run the build again.") | Out-Null
    if (-not $NoPause) { Wait-ForKeyPress }
    exit 1
}
Write-Ok "All requested targets built successfully"
Write-Ok "Published output: $PublishRoot"
