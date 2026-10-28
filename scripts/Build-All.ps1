param(
    [string[]]$Targets = @('uwp-arm32', 'uwp-arm32-l9d3d11', 'uwp-x64', 'uwp-x86', 'win32', 'win64'),
    [string]$UnityPath,
    [string]$MSBuildPath,
    [switch]$NoPublish,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

# Accept both "-Targets win32,win64" and "-Targets win32 win64"; the first form arrives as a
# single string when the script is started with `powershell -File`.
$Targets = @($Targets | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$knownTargets = @('win64', 'win32', 'uwp-x64', 'uwp-x86', 'uwp-arm32', 'uwp-arm32-l9d3d11')
$unknownTargets = @($Targets | Where-Object { $knownTargets -notcontains $_ })
if ($unknownTargets.Count -gt 0) {
    throw "Unknown target(s): $($unknownTargets -join ', '). Valid targets: $($knownTargets -join ', ')"
}
if ($Targets.Count -eq 0) { throw 'No build target selected.' }

$scripts = @{
    'win64'      = 'Build-Win64.ps1'
    'win32'      = 'Build-Win32.ps1'
    'uwp-x64'    = 'Build-UWP-x64.ps1'
    'uwp-x86'    = 'Build-UWP-x86.ps1'
    'uwp-arm32'  = 'Build-UWP-ARM32.ps1'
    # Packs the ARM32 appx, so it belongs after uwp-arm32. When that package is
    # missing the L9 script builds it itself.
    'uwp-arm32-l9d3d11' = 'Build-UWP-ARM32-L9D3D11-RT-Fix.ps1'
}

Write-Step "Building: $($Targets -join ', ')"
$started = Get-Date
$failed = @()

foreach ($target in $Targets) {
    $script = Join-Path $PSScriptRoot $scripts[$target]
    Write-Step "Target: $target"
    try {
        # -NoPause: the child scripts must not stop for a key press, Build-All does that once at the end.
        $parameters = @{
            UnityPath   = $UnityPath
            MSBuildPath = $MSBuildPath
            NoPublish   = $NoPublish
            NoPause     = $true
        }

        if ($target -eq 'uwp-arm32-l9d3d11' -and $NoPublish) {
            # The L9 package is built from the ARM32 appx, and -NoPublish means the
            # published copy does not exist. Take the test package MSBuild produced.
            $packageFolder = Get-LatestAppPackage -Root (Join-Path $BuildsRoot 'WSA\AppPackages') -Architecture 'ARM'
            $sourceAppx = $null
            if ($packageFolder) {
                $sourceAppx = Get-ChildItem -LiteralPath $packageFolder.FullName -Filter '*.appx' -File -ErrorAction SilentlyContinue |
                    Sort-Object LastWriteTime -Descending | Select-Object -First 1
            }
            if (-not $sourceAppx) {
                throw 'no ARM appx under Builds\WSA\AppPackages - build uwp-arm32 first or drop -NoPublish'
            }
            Write-Host "  Source appx: $($sourceAppx.FullName)"
            $parameters['AppxPath'] = $sourceAppx.FullName
        }

        & $script @parameters
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
if ($NoPublish) {
    Write-Ok 'Publishing was skipped (-NoPublish): outputs stay in Builds\ (and in %TEMP% for the L9 package)'
} else {
    Write-Ok "Published output: $PublishRoot"
}
