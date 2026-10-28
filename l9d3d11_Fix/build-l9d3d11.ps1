# build-l9d3d11.ps1
#
# Rebuilds l9d3d11.dll (the ARM32 Direct3D 11 feature-level 9_1 shim) from src\.
# The compiler is bundled in llvm-rt\; only the Microsoft headers/libraries come
# from the installed Visual Studio + Windows SDK.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\build-l9d3d11.ps1
#   powershell -ExecutionPolicy Bypass -File .\build-l9d3d11.ps1 -Force
#
# The output replaces .\l9d3d11.dll, which is the file the packaging script copies
# into the appx. Rebuilding is only needed after editing src\ or replacing the
# compiler; the shipped DLL is already built from the shipped sources.

[CmdletBinding()]
param(
    # Rebuild even if l9d3d11.dll already exists.
    [switch]$Force,
    # Write the DLL somewhere else (default: next to this script).
    [string]$OutputPath,
    # Windows Kits root (default: C:\Program Files (x86)\Windows Kits\10).
    [string]$SdkRoot,
    # SDK version with ARM32 libraries (default: 10.0.19041.0 if present, else the newest).
    [string]$SdkVersion,
    # MSVC include directory (default: found through vswhere).
    [string]$MsvcInclude
)

$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'patch-pe.ps1')

$SrcDir  = Join-Path $Here 'src'
$Clang   = Join-Path $Here 'llvm-rt\bin\clang.exe'
$LldLink = Join-Path $Here 'llvm-rt\bin\lld-link.exe'
$Source  = Join-Path $SrcDir 'l9shim.c'
$Def     = Join-Path $SrcDir 'l9shim.def'

if (-not $OutputPath) { $OutputPath = Join-Path $Here 'l9d3d11.dll' }
if ((Test-Path -LiteralPath $OutputPath) -and -not $Force) {
    Write-Host "Already built: $OutputPath (use -Force to rebuild)"
    exit 0
}

function Write-Step { param([string]$Text) Write-Host ""; Write-Host "=== $Text ===" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Text) Write-Host "  OK: $Text" -ForegroundColor Green }
function Write-Info { param([string]$Text) Write-Host "  $Text" }

function Find-WindowsSdk {
    param([string]$Root, [string]$Version)

    $roots = @()
    if ($Root) { $roots += $Root }
    $roots += (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10')
    $roots += (Join-Path $env:ProgramFiles 'Windows Kits\10')
    $roots = @($roots | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique)

    foreach ($r in $roots) {
        if ($Version) {
            $versions = @($Version)
        } else {
            $libRoot = Join-Path $r 'Lib'
            if (-not (Test-Path -LiteralPath $libRoot)) { continue }
            $versions = @(Get-ChildItem -LiteralPath $libRoot -Directory |
                Sort-Object Name -Descending | ForEach-Object { $_.Name })
            # The ARM32 libraries disappeared after 10.0.22621; prefer the version the
            # project builds against when it is installed.
            $versions = @('10.0.19041.0') + @($versions | Where-Object { $_ -ne '10.0.19041.0' })
        }
        foreach ($v in $versions) {
            if ((Test-Path -LiteralPath (Join-Path $r "Lib\$v\um\arm\kernel32.lib")) -and
                (Test-Path -LiteralPath (Join-Path $r "Lib\$v\um\arm\armrt.lib")) -and
                (Test-Path -LiteralPath (Join-Path $r "Include\$v\um\windows.h"))) {
                return [pscustomobject]@{ Root = $r; Version = $v }
            }
        }
    }
    throw ("Windows SDK with ARM32 libraries not found. Install the Windows 10 SDK 10.0.19041 " +
           "with the 'Desktop C++ for ARM' component, or pass -SdkRoot/-SdkVersion.")
}

function Find-MsvcInclude {
    param([string]$Explicit)

    if ($Explicit) {
        if (-not (Test-Path -LiteralPath (Join-Path $Explicit 'vcruntime.h'))) {
            throw "MSVC include directory has no vcruntime.h: $Explicit"
        }
        return $Explicit
    }
    if ($env:BPRE_MSVCINC) {
        if (Test-Path -LiteralPath (Join-Path $env:BPRE_MSVCINC 'vcruntime.h')) { return $env:BPRE_MSVCINC }
        throw "BPRE_MSVCINC is set but has no vcruntime.h: $env:BPRE_MSVCINC"
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $installs = @()
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null
        if ($found) { $installs += ($found | Select-Object -First 1) }
        $any = & $vswhere -latest -products * -property installationPath 2>$null
        if ($any) { $installs += ($any | Select-Object -First 1) }
    }
    $installs += @(
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\Community'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\Professional'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\Enterprise'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\BuildTools')
    )

    foreach ($vs in @($installs | Where-Object { $_ } | Select-Object -Unique)) {
        $tools = Join-Path $vs 'VC\Tools\MSVC'
        if (-not (Test-Path -LiteralPath $tools)) { continue }
        foreach ($v in @(Get-ChildItem -LiteralPath $tools -Directory | Sort-Object Name -Descending)) {
            $inc = Join-Path $v.FullName 'include'
            if (Test-Path -LiteralPath (Join-Path $inc 'vcruntime.h')) { return $inc }
        }
    }
    throw ("MSVC include directory not found. Install Visual Studio 2022 with 'Desktop development with C++' " +
           "or set BPRE_MSVCINC / pass -MsvcInclude.")
}

function Test-ShimDll {
    param([string]$Path)

    $info = Get-PeInfo -Bytes ([System.IO.File]::ReadAllBytes($Path))
    if ($info.Machine -ne 0x1C4) { throw ("not an ARM32 image: machine 0x{0:X4}" -f $info.Machine) }
    $imports = @(Get-PeImports -Info $info)
    $unexpected = @($imports | Where-Object { $_ -ine 'KERNEL32.dll' })
    if ($unexpected.Count -gt 0) { throw ("unexpected imports: {0}" -f ($unexpected -join ', ')) }
    $exports = @(Get-PeExports -Info $info)
    foreach ($need in @('D3D11CreateDevice', 'CreateDirect3D11SurfaceFromDXGISurface', 'CreateDirect3D11DeviceFromDXGIDevice')) {
        if (-not ($exports | Where-Object { $_ -ceq $need })) { throw "missing export: $need" }
    }
    return [pscustomobject]@{ Imports = $imports; Exports = $exports; Size = (Get-Item -LiteralPath $Path).Length }
}

try {
    Write-Step 'Building l9d3d11.dll (ARM32)'

    foreach ($p in @($Clang, $LldLink, $Source, $Def)) {
        if (-not (Test-Path -LiteralPath $p)) { throw "Required file is missing: $p" }
    }

    $sdk = Find-WindowsSdk -Root $SdkRoot -Version $SdkVersion
    $msvcInc = Find-MsvcInclude -Explicit $MsvcInclude
    Write-Info "Compiler:  $Clang"
    Write-Info "Linker:    $LldLink"
    Write-Info "SDK:       $($sdk.Root) $($sdk.Version)"
    Write-Info "MSVC inc:  $msvcInc"

    $verOut = & $Clang --version 2>&1
    $verCode = $LASTEXITCODE
    $ver = @($verOut)[0]
    if ($verCode -ne 0) { throw "clang does not run: $ver" }
    Write-Info "Version:   $ver"
    if ($ver -notmatch '23\.1\.1-rt2\.1') {
        Write-Warning "The bundled compiler is not the expected rt2.1 build. Syntax/validation will decide; see llvm-rt\README.md."
    }

    $obj = Join-Path $env:TEMP ("l9shim-{0}.obj" -f $PID)
    $impLib = Join-Path $env:TEMP ("l9shim-{0}.lib" -f $PID)
    $inc = Join-Path $sdk.Root "Include\$($sdk.Version)"
    $umArm = Join-Path $sdk.Root "Lib\$($sdk.Version)\um\arm"

    $clangArgs = @(
        '--target=thumbv7-pc-windows-msvc',
        '-march=armv7-a', '-mfpu=neon', '-mfloat-abi=hard',
        '-O2', '-ffreestanding', '-fno-builtin', '-fno-stack-protector',
        '-Wall', '-Wno-microsoft-cast', '-Wno-ignored-attributes',
        '-D_WIN32_WINNT=0x0603', '-DWINVER=0x0603',
        '-isystem', $msvcInc,
        '-isystem', (Join-Path $inc 'um'),
        '-isystem', (Join-Path $inc 'shared'),
        '-isystem', (Join-Path $inc 'ucrt'),
        '-c', $Source, '-o', $obj
    )
    & $Clang @clangArgs
    if ($LASTEXITCODE -ne 0) { throw "clang failed with exit code $LASTEXITCODE" }
    Write-Ok 'Compiled l9shim.c'

    $linkArgs = @(
        '/dll', '/machine:arm', '/subsystem:windows,6.02', '/entry:DllMain',
        '/nodefaultlib', '/brepro', '/timestamp:0', ("/def:$Def"), ("/out:$OutputPath"),
        # lld writes an import library next to the DLL by default; keep it out of the tree.
        ("/implib:$impLib"),
        $obj, (Join-Path $umArm 'kernel32.lib'), (Join-Path $umArm 'armrt.lib')
    )
    & $LldLink @linkArgs
    if ($LASTEXITCODE -ne 0) { throw "lld-link failed with exit code $LASTEXITCODE" }
    Remove-Item -LiteralPath $obj -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $impLib -Force -ErrorAction SilentlyContinue

    $check = Test-ShimDll -Path $OutputPath
    $sha = (Get-FileHash -LiteralPath $OutputPath -Algorithm SHA256).Hash
    Write-Ok ("l9d3d11.dll: {0} bytes, sha256 {1}" -f $check.Size, $sha)
    Write-Ok ("exports: {0}" -f ($check.Exports -join ', '))
    Write-Ok ("imports: {0}" -f ($check.Imports -join ', '))
    Write-Host ""
    Write-Host "Rebuilt: $OutputPath" -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "BUILD FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
