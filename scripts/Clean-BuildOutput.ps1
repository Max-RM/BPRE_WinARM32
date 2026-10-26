param(
    [switch]$Deep,       # also delete the Unity Library cache (full reimport, slow)
    [switch]$Archives,   # also delete Archives
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

Assert-Path -Path (Join-Path $ProjectRoot 'Assets') -Label 'Unity Assets folder'

function Remove-Target {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "  skip: $Label (not found)"
        return
    }
    if ($DryRun) {
        Write-Host "  would delete: $Label - $Path"
        return
    }
    Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  removed: $Label - $Path"
}

Write-Step 'Cleaning build outputs'
Remove-Target -Path (Join-Path $BuildsRoot 'Win64')       -Label 'Win64 build'
Remove-Target -Path (Join-Path $BuildsRoot 'Win32')       -Label 'Win32 build'
Remove-Target -Path (Join-Path $BuildsRoot 'WSA')         -Label 'UWP ARM32 build'
Remove-Target -Path (Join-Path $BuildsRoot 'WSA64')       -Label 'UWP x64 build'
Remove-Target -Path (Join-Path $BuildsRoot 'WSA86')       -Label 'UWP x86 build'
Remove-Target -Path (Join-Path $BuildsRoot 'obj')         -Label 'IL2CPP object cache'
Remove-Target -Path $PublishRoot                          -Label 'Publish folder'

Write-Step 'Cleaning logs'
Remove-Target -Path (Join-Path $ProjectRoot 'Logs')       -Label 'Unity logs'
Remove-Target -Path (Join-Path $ProjectRoot 'Temp')       -Label 'Unity temp'
Remove-Target -Path $BuildLogDir -Label 'Build script logs'

if ($Deep) {
    Write-Step 'Cleaning Unity Library (deep clean)'
    Remove-Target -Path (Join-Path $ProjectRoot 'Library') -Label 'Unity Library'
}

if ($Archives) {
    Write-Step 'Cleaning archives'
    Remove-Target -Path (Join-Path $ProjectRoot 'Archives') -Label 'Archives'
}

Write-Step 'Clean complete'
