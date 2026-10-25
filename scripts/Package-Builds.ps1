param(
    [ValidateSet('win64', 'win32', 'uwp-x64', 'uwp-arm32')]
    [string[]]$Items = @('win64', 'win32', 'uwp-x64', 'uwp-arm32'),
    [string]$Version,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Build-Common.ps1')

if (-not $Version) {
    $projectSettings = Join-Path $ProjectRoot 'ProjectSettings\ProjectSettings.asset'
    if (Test-Path -LiteralPath $projectSettings) {
        $line = Select-String -LiteralPath $projectSettings -Pattern '^\s*bundleVersion:\s*(.+)$' |
            Select-Object -First 1
        if ($line) { $Version = $line.Matches[0].Groups[1].Value.Trim() }
    }
}
if (-not $Version) { $Version = Get-Date -Format 'yyyyMMdd' }

if (-not $OutputDir) { $OutputDir = Join-Path $ProjectRoot 'Archives' }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Prefer 7-Zip; fall back to Compress-Archive.
$SevenZip = $null
foreach ($candidate in @('7z', '7za')) {
    $cmd = Get-Command $candidate -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { $SevenZip = $cmd.Source; break }
}
if ($SevenZip) {
    Write-Host "Using 7-Zip: $SevenZip" -ForegroundColor Green
} else {
    Write-Warn '7-Zip not found on PATH, falling back to Compress-Archive (slow)'
}

$sources = @{
    'win64'     = @{ Dir = (Join-Path $PublishRoot 'Win64');     Name = 'win64' }
    'win32'     = @{ Dir = (Join-Path $PublishRoot 'Win32');     Name = 'win32' }
    'uwp-x64'   = @{ Dir = (Join-Path $PublishRoot 'UWP-x64');   Name = 'uwp-x64' }
    'uwp-arm32' = @{ Dir = (Join-Path $PublishRoot 'UWP-ARM32'); Name = 'uwp-arm32' }
}

function New-Archive {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$ArchiveName
    )

    if (-not (Test-Path -LiteralPath $SourceDir)) {
        Write-Warn "skipped (not found): $SourceDir"
        return
    }

    $archivePath = Join-Path $OutputDir "$ArchiveName.zip"
    if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }

    Write-Host "  Packing: $SourceDir"
    Write-Host "        -> $archivePath"

    if ($SevenZip) {
        Push-Location -LiteralPath $SourceDir
        try {
            & $SevenZip a -tzip -mx=9 -y $archivePath '.\*' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "7-Zip failed with exit code $LASTEXITCODE" }
        } finally {
            Pop-Location
        }
    } else {
        Compress-Archive -Path (Join-Path $SourceDir '*') -DestinationPath $archivePath -CompressionLevel Optimal
    }
}

Write-Step "Packaging version $Version (from Publish)"

foreach ($item in $Items) {
    $source = $sources[$item]
    New-Archive -SourceDir $source.Dir -ArchiveName "BPRE-$Version-$($source.Name)"
}

Write-Step 'Packaging complete'
Get-ChildItem -LiteralPath $OutputDir -Filter '*.zip' | Sort-Object Name |
    Select-Object Name, @{n = 'SizeMB'; e = { [math]::Round($_.Length / 1MB, 1) } } |
    Format-Table -AutoSize
