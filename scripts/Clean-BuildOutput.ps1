param(
    [switch]$Deep,       # also delete the Unity Library cache (full reimport, slow)
    [switch]$Archives,   # accepted for compatibility - Archives is cleaned anyway
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

Assert-Path -Path (Join-Path $ProjectRoot 'Assets') -Label 'Unity Assets folder'

function Remove-Target {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        # Publish and Archives stay as folders - only their contents are removed, so an
        # Explorer window, a shortcut or a script that points at them keeps working.
        [switch]$KeepFolder
    )
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "  skip: $Label (not found)"
        return
    }
    if ($DryRun) {
        if ($KeepFolder) {
            $entries = @(Get-ChildItem -LiteralPath $Path -Force -ErrorAction SilentlyContinue)
            Write-Host "  would empty: $Label - $Path ($($entries.Count) entries)"
        } else {
            Write-Host "  would delete: $Label - $Path"
        }
        return
    }
    if ($KeepFolder) {
        $children = @(Get-ChildItem -LiteralPath $Path -Force -ErrorAction SilentlyContinue)
        foreach ($child in $children) {
            Remove-Item -LiteralPath $child.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
        # SilentlyContinue also hides files that are locked or in use, so check the folder
        # afterwards instead of reporting a clean sweep that did not happen.
        $left = @(Get-ChildItem -LiteralPath $Path -Force -ErrorAction SilentlyContinue)
        if ($left.Count -gt 0) {
            Write-Warn "$Label was not emptied: $($left.Count) of $($children.Count) entries are still there in $Path"
            foreach ($item in $left) { Write-Host "    still there: $($item.Name)" }
            return
        }
        Write-Host "  emptied: $Label - $Path ($($children.Count) entries)"
        return
    }
    Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $Path) {
        Write-Warn "$Label could not be removed completely: $Path"
        foreach ($item in @(Get-ChildItem -LiteralPath $Path -Force -ErrorAction SilentlyContinue)) {
            Write-Host "    still there: $($item.Name)"
        }
        return
    }
    Write-Host "  removed: $Label - $Path"
}

Write-Step 'Cleaning build outputs'
Remove-Target -Path (Join-Path $BuildsRoot 'Win64')       -Label 'Win64 build'
Remove-Target -Path (Join-Path $BuildsRoot 'Win32')       -Label 'Win32 build'
Remove-Target -Path (Join-Path $BuildsRoot 'WSA')         -Label 'UWP ARM32 build'
Remove-Target -Path (Join-Path $BuildsRoot 'WSA64')       -Label 'UWP x64 build'
Remove-Target -Path (Join-Path $BuildsRoot 'WSA86')       -Label 'UWP x86 build'
Remove-Target -Path (Join-Path $BuildsRoot 'obj')         -Label 'IL2CPP object cache'
Remove-Target -Path $PublishRoot                          -Label 'Publish folder' -KeepFolder

Write-Step 'Cleaning logs'
Remove-Target -Path (Join-Path $ProjectRoot 'Logs')       -Label 'Unity logs'
Remove-Target -Path (Join-Path $ProjectRoot 'Temp')       -Label 'Unity temp'
Remove-Target -Path $BuildLogDir -Label 'Build script logs'

if ($Deep) {
    Write-Step 'Cleaning Unity Library (deep clean)'
    Remove-Target -Path (Join-Path $ProjectRoot 'Library') -Label 'Unity Library'
}

# Archives holds the zips written by Package-Builds.ps1. They are build output, so they are
# removed here as well; the folder itself stays, so the next Package-Builds run and any
# shortcut that points at it keep working. (-Archives is still accepted, but no longer needed.)
Write-Step 'Cleaning archives'
Remove-Target -Path (Join-Path $ProjectRoot 'Archives') -Label 'Archives' -KeepFolder

Write-Step 'Clean complete'
