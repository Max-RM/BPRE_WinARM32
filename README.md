# Bad Piggies :: Rebooted Edition — Windows ARM32 port

A **Windows port** of [Bad Piggies :: Rebooted Edition](https://github.com/Goggs77/bp-rebooted), a community-driven
modification of Bad Piggies.

The original mod ships as an Android build that only runs through emulation on Windows. This repository contains a
self-contained Unity project that builds the very same game natively for Windows, including the **ARM32 target that
never existed before this port** — and a **Direct3D 9_1 layer that brings it to Surface RT and Surface 2**.

**Ported by MaxRM (TNT ENTERTAINMENT Inc) with DeepSeek V4.1 Flash Free — October 2026.**

## Supported systems

| Target | System | Backend | Status |
|---|---|---|---|
| **UWP ARM32** | Windows 10 Mobile 10.0.15063+ (tested on HP Elite x3), Windows 10 ARM32 15035 (the leaked 32-bit desktop ARM build) and Windows 10/11 ARM64 (ARM64 devices run it through ARM32 backward compatibility) | IL2CPP | main target of this port |
| **UWP ARM32 + l9d3d11 (TegraPatch)** | **Surface RT / Surface 2** on Windows 10 ARM32 build 15035 — the GPU only offers Direct3D 9_1 | IL2CPP + Direct3D 11 → 9_1 shim | supported through the separate package |
| UWP x64 | Windows 10/11 on x64 (desktop mode) | IL2CPP | works |
| UWP x86 | 32-bit Windows 10/11 and the x86 test target | IL2CPP | works; useful as an extra 32-bit test build |
| Windows x64 | Windows 7 SP1 / 8 / 8.1 / 10 / 11 (standalone, no sideloading) | Mono | works |
| Windows x86 | 32-bit Windows 7 SP1 / 8 / 8.1 / 10 / 11 (standalone) | Mono | works; also the local test target for the 32-bit code path |

The 32-bit builds (UWP ARM32 and Windows x86)
show a dedicated splash screen (`Assets/Resources/innovation/Innovation_Background_WinARM32.png`) with
the porting credits; the x64 builds keep the original splash. All builds use the same Unity 2018.4 IL2CPP/Mono player code, so gameplay is
identical — only the packaging and the rendering backend differ.

### Surface RT and Surface 2

Both tablets are supported once they run **Windows 10 ARM32 build 15035** — the leaked 32-bit desktop ARM build.
**Windows RT 8.1 itself is not supported**: the Unity 2018.4 UWP player needs APIs that only exist in Windows 10.

The stock `BPRE_UWP-ARM32` package is not usable on these tablets: the Tegra 3 / Tegra 4 driver exposes only
Direct3D 9_1, while the Unity 2018.4 UWP player asks for feature level 10.0. The
**`BPRE_UWP-ARM32-L9D3D11`** package fixes that: `l9d3d11.dll` sits between Unity and Direct3D and

- makes `D3D11CreateDevice` succeed with a 9_1 device,
- replaces Unity's SM4/SM5 shaders with prebuilt `*_level_9_1` variants from `Data\bp\l9_replacements.bin`,
- sanitises descriptors, formats and caps to the 10Level9 rules.

The approach comes from [Max-RM/Win32Craft_ARM](https://github.com/Max-RM/Win32Craft_ARM), where it was designed and
proven on a Surface 2. How the layer is built, which file came from where, how to rebuild it and what its limitations
are is described in [`l9d3d11_Fix/README.md`](l9d3d11_Fix/README.md).

### Architecture of the port

| Part | Notes |
|---|---|
| Unity project | Downgraded from 2021.3 to **2018.4.36f1** (the last version that still targets Windows 10 Mobile ARM32) |
| UWP ARM32 | IL2CPP, MSVC ARM toolset, packaged as `.appx` with the ARM32 splash patch applied at runtime |
| UWP ARM32 (Surface RT/2) | the same package repacked with `l9d3d11.dll`, the level-9 shader table, an app-local VC++ ARM runtime and no `Microsoft.VCLibs` dependency |
| UWP x64 | IL2CPP, packaged as `.appx` (desktop devices) |
| UWP x86 | IL2CPP, 32-bit package (test target for the 32-bit code path) |
| Win64 | Mono standalone player, no package or certificate needed |
| Win32 | 32-bit Mono standalone player; shares the 32-bit code path (`IntPtr.Size == 4`, ARM32 splash) with the ARM package |
| UWP packaging | `Package.appxmanifest` + MSIX-style `.appx`, signed with one self-signed certificate (`BPRE_UWP.cer`) |
| Direct3D 9_1 layer | `l9d3d11.dll` + `l9_replacements.bin`, built from `l9d3d11_Fix\src` with clang/LLD for ARM |

## Requirements

Building **any** target requires:

| Component | Version / notes |
|---|---|
| Unity | **2018.4.36f1** — the project was downgraded from 2021.3, newer editors will not upgrade it cleanly |
| Visual Studio | 2022 with the **Universal Windows Platform development** workload **and the MSVC v143 ARM build tools** (`Microsoft.VisualStudio.Component.VC.Tools.ARM`) |
| Windows SDK | **10.0.19041.0** (also provides `makeappx.exe` / `signtool.exe` for the Surface RT package) |
| MSVC toolset | **MSVC v143** — any version that ships ARM32 (see the note below) |
| PowerShell | 5.1 (bundled with Windows 10/11) |
| 7-Zip | optional, only used for packing archives |

Building the **Surface RT / Surface 2** package additionally needs nothing: the repository already contains the
prebuilt `l9d3d11.dll`, the shader table, the ARM VC++ runtime and the LLVM toolchain used to build the shim, so
`Build-UWP-ARM32-L9D3D11-RT-Fix.ps1` is a repack-and-sign step. The toolchain in `l9d3d11_Fix\llvm-rt` is only
needed when the shim itself is rebuilt (about 150 MB, see [`l9d3d11_Fix/README.md`](l9d3d11_Fix/README.md)).

Unity 2018.4.36f1 downloads (the editor is no longer listed on the main page):

- Unity download archive: <https://unity.com/releases/editor/archive>
- Direct installer: <https://download.unity3d.com/download_unity/6cd387d23174/Windows64EditorInstaller/UnitySetup64-2018.4.36f1.exe>
- Backup mirror (Telegram): <https://t.me/TNT_ENTERTAINMENT_inc/165114>

During installation select the **Windows Build Support (Mono)** and **Universal Windows Platform Build Support**
(IL2CPP) modules for the editor.

> **ARM32 note.** The ARM32 target needs the *ARM build tools* component in Visual Studio; the
> "Desktop development with C++" and "Universal Windows Platform development" workloads do not
> install it by default, and without it IL2CPP stops with
> `architecture ARMv7 is not supported by this Visual Studio version`. Add
> **MSVC v143 - VS 2022 C++ ARM build tools** from the Visual Studio Installer (Individual
> components → Compilers, build tools and runtimes), or run:
>
> ```powershell
> & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\setup.exe" modify `
>     --installPath "C:\Program Files\Microsoft Visual Studio\2022\Community" `
>     --add Microsoft.VisualStudio.Component.VC.Tools.ARM --quiet --norestart
> ```
>
> This installs `bin\Hostx64\arm` and `lib\arm` for the MSVC toolsets on the machine. IL2CPP
> resolves the toolchain through the *default toolset* file
> (`VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt`); if that version has no
> `bin\Hostx64\arm`, point it at one that does.

The build scripts check all of the above before starting. When a Visual Studio component is missing they offer to
install it through the Visual Studio Installer; when something has to be installed manually (Unity, the UWP module)
a dialog shows the download links. A failed build never closes the window immediately: the error is shown and the
script waits for a key press.

## Building

All builds run from the command line, no need to open the Unity editor.

```powershell
# UWP ARM32 (main target)          -> Builds\WSA   + Publish\BPRE_UWP-ARM32
.\scripts\Build-UWP-ARM32.ps1

# UWP ARM32 for Surface RT/2       -> repacks the ARM32 appx
#                                    + Publish\BPRE_UWP-ARM32-L9D3D11
.\scripts\Build-UWP-ARM32-L9D3D11-RT-Fix.ps1

# UWP x64                          -> Builds\WSA64 + Publish\BPRE_UWP-x64
.\scripts\Build-UWP-x64.ps1

# UWP x86 (32-bit test target)     -> Builds\WSA86 + Publish\BPRE_UWP-x86
.\scripts\Build-UWP-x86.ps1

# Windows x64 standalone           -> Builds\Win64 + Publish\BPRE_Win64
.\scripts\Build-Win64.ps1

# Windows x86 standalone           -> Builds\Win32 + Publish\BPRE_Win32
.\scripts\Build-Win32.ps1

# all six, in one go (ARM32 first, the Surface RT package reuses it)
.\scripts\Build-All.ps1

# only some targets
.\scripts\Build-All.ps1 -Targets uwp-arm32,uwp-arm32-l9d3d11
```

The Surface RT / Surface 2 script needs the ARM32 package: it uses `Publish\BPRE_UWP-ARM32` when it is there and
otherwise builds it first. It does not touch the Unity project, so it takes about a minute instead of a full build.

Unity and MSBuild are auto-detected. Override them with `-UnityPath` / `-MSBuildPath` or the
`UNITY_EDITOR` / `MSBUILD_EXE` environment variables. `-NoPause` skips the "press any key" wait (useful for CI).

Maintenance helpers:

```powershell
# remove build outputs and empty Publish + Archives (both folders are kept)
.\scripts\Clean-BuildOutput.ps1
# ... and the Unity Library cache too (next build reimports everything)
.\scripts\Clean-BuildOutput.ps1 -Deep

# zip the Publish folders into Archives\BPRE-<version>-*.zip
.\scripts\Package-Builds.ps1
```

See [`scripts/README.md`](scripts/README.md) for the details of every script.

## Output layout

```
Builds\                          intermediate build trees (not published, git-ignored)
Publish\
  BPRE_Win64\                    the whole Windows x64 game folder + Open-Saves.bat
  BPRE_Win32\                    the same game for 32-bit Windows
  BPRE_UWP-x64\
    Bad_PiggiesRebooted_1.0.0.0_x64.appx
    Bad_PiggiesRebooted_1.0.0.0_x64.cer
    Dependencies\Microsoft.VCLibs.x64.14.00.appx
    Install.bat / Install.ps1 / Open-Saves.bat / README.txt
  BPRE_UWP-x86\                  same set of files, x86 package + VCLibs.x86
  BPRE_UWP-ARM32\                same set of files, ARM32 package + VCLibs.arm
  BPRE_UWP-ARM32-L9D3D11\        Surface RT / 2 package + BPRE_UWP.cer, PATCH_REPORT.txt,
                                 no Dependencies folder (the VC++ ARM runtime is app-local)
Archives\                        zip files created by Package-Builds.ps1
  BPRE-<version>-win64.zip / -win32.zip
  BPRE-<version>-uwp-x64.zip / -uwp-x86.zip
  BPRE-<version>-uwp-ARM32.zip / -uwp-ARM32-TegraPatch.zip
```

`<version>` is `bundleVersion` from `ProjectSettings\ProjectSettings.asset` (`Delta-Epsilon` at the moment).

### Package names

Package file names are normalised when they are published, so they survive FAT32 cards, old Explorer dialogs and
CMD Injector:

| Build | Published file |
|---|---|
| UWP x64 | `Bad_PiggiesRebooted_<version>_x64.appx` |
| UWP x86 | `Bad_PiggiesRebooted_<version>_x86.appx` |
| UWP ARM32 | `Bad_PiggiesRebooted_<version>_ARM32.appx` |
| UWP ARM32, Surface RT/2 | `Bad_PiggiesRebooted_<version>_ARM32_TegraPatch.appx` |

Spaces become underscores (`Bad PiggiesRebooted_...` → `Bad_PiggiesRebooted_...`), MSBuild's `_Win32` becomes
`_x86` and `_ARM` becomes `_ARM32`. Only the file name changes: the identity, the manifest and the signature inside
the package are untouched, so either ARM32 package replaces the other and both use the same save folder.

### Windows x64 (standalone)

Open `Publish\BPRE_Win64\BadPiggiesRebooted.exe`. No installation, certificate or dependency is required.

### Windows x86 (standalone, 32-bit)

Same as above with `Publish\BPRE_Win32\BadPiggiesRebooted.exe`. This build runs on 32-bit Windows and is
also the quickest way to see the ARM32 splash and behaviour on a normal PC, because it takes the same
32-bit code path as the UWP ARM32 package.

### Desktop Windows 10 / 11 (UWP)

1. Copy the whole `BPRE_UWP-x64` folder (or `BPRE_UWP-ARM32` on a Windows-on-ARM device) to the target machine.
2. Run **`Install.bat`** and confirm the administrator prompt.
3. The script trusts the certificate, installs `Microsoft.VCLibs.140.00`, then the game, and offers to start it.

`Install.bat` is only a wrapper that elevates and calls `Install.ps1`; the same steps can be done manually:
import the `.cer` into **Local Machine → Trusted People**, install `Dependencies\*.appx`, then the game `.appx`.
`sideloading` (or Developer mode) must be enabled in Settings → Update & security → For developers.

The ARM32 package also installs on the leaked **Windows 10 ARM32 desktop build 15035**. That build
runs PowerShell in Constrained Language Mode, so `Install.ps1` deliberately avoids `Add-Type`,
`New-Object` and `System.IO.Compression`; the installer reads the version and architecture from the file name
instead of the package manifest. If `Install.bat` still cannot start, run
`powershell -ExecutionPolicy Bypass -File .\Install.ps1` as administrator.

### Surface RT / Surface 2 (UWP, l9d3d11)

Use the separate package — the regular ARM32 one only falls back to software rendering there.

1. Copy the `BPRE_UWP-ARM32-L9D3D11` folder to the tablet (USB stick, network share or `\\<device>\C$`).
2. Run **`Install.bat`**. It trusts `BPRE_UWP.cer`, installs the package (there is no VCLibs dependency to install
   first) and offers to start the game.
3. If the package refuses to install, unpack the `.appx` and run **`Install-Loose.bat`** from the unpacked folder
   instead — see *Installing from an unpacked folder* below. That path needs Developer Mode but no certificate.

### Installing from an unpacked folder

The Surface RT / 2 package contains `Install-Loose.bat`, `Install-Loose.ps1`, `Open-Saves.bat` and
`README-LOOSE.txt` in addition to the game. To install it that way, unpack the `.appx` (7-Zip, Explorer or
`makeappx unpack`) and run `Install-Loose.bat`:

- it registers the folder in place (`Add-AppxPackage -Register`), so the folder must stay where it is;
- it removes `AppxSignature.p7x` and the block-map leftovers, which a registered folder must not contain;
- it repairs names that 7-Zip and Explorer leave percent-encoded — `Bad%20PiggiesRebooted.exe`,
  `unity%20default%20resources` — because the manifest references the decoded names and the appx runtime itself
  decodes them. `makeappx unpack` already gives the correct names, so the step does nothing there;
- it refuses to run when the game is already installed, because Windows cannot replace an installed copy from a
  loose folder.

Developer Mode must be enabled. The scripts stay Constrained Language Mode safe, so they also work on build 15035.

### Windows 10 Mobile

Windows 10 Mobile cannot run `Install.bat`, so the package is installed by hand:

1. Enable **Developer mode** in Settings → Update & security → For developers.
2. Copy the `BPRE_UWP-ARM32` folder to the phone.
3. Tap the `.cer` file and install it into **Trusted People** (or use the certificate import in CMD Injector).
4. Deploy `Dependencies\Microsoft.VCLibs.arm.14.00.appx` with **CMD Injector**, then the game `.appx`.
   Alternatively use the **Device Portal** (`http://<phone-ip>:80` → Apps → Deploy), uploading the dependency
   first. CMD Injector requires the usual interop unlock of the phone.
5. Launch **Bad Piggies::Rebooted** from the app list.

The complete instructions, including error codes (`0x800B0109`, `0x80073CF3`, …) are in the `README.txt` that
ships inside every `Publish\BPRE_UWP-*` folder.

## Saves and logs

Every published folder contains `Open-Saves.bat`, which opens that build's save folder in Explorer. It never changes
anything — it exists so the progress can be copied somewhere else before an uninstall, because removing a package or
the game folder deletes it.

| Build | Save folder |
|---|---|
| UWP (x64, x86, ARM32, TegraPatch) | `%LOCALAPPDATA%\Packages\BadPiggiesRebooted_<hash>\LocalState` |
| Windows x64 / x86 | `%USERPROFILE%\AppData\LocalLow\_Imaginary_\Bad Piggies__Rebooted` |

`<hash>` is the publisher part of the package family name and comes from the signing certificate:
`x0xr9mv5edyrp` for the certificate in this repository, something else (for example `...hj`) for a package signed
with another one. The batch file therefore does not hard-code it: it tries the known name first, then any
`BadPiggiesRebooted_*` package folder, preferring one that already has `LocalState`. The Windows standalone script is
generated at publish time from `ProjectSettings.asset` (`BuildSupport\Win\Open-Saves.bat`), so renaming the company
or the product keeps it correct.

The UWP saves are `Progress.dat`, `Progress.dat.bak`, `Settings.xml`, `playerprefs.dat` and the `contraptionsB`
folder. Logs are one level up, in `TempState\` (`UnityPlayer.log`); the Direct3D 9_1 layer writes `l9shim.log`
(GPU, obtained feature level, swapped shaders, errors) somewhere under the same package folder — search for it there.

## Signing certificate

Every UWP package — x64, x86, ARM32 and the Surface RT/2 repack — is signed with one self-signed certificate:

| | |
|---|---|
| File | `BuildSupport\UWP\BPRE_UWP.pfx` (password `bpre`), public part in `BPRE_UWP.cer` |
| Subject | `CN=TNT ENTERTAINMENT Inc` |
| Thumbprint (SHA-1) | `C54C1A84A2EB4610BDF006D97A7A01C4F4FE718D` |
| Validity | 26.10.2026 – 26.10.2036 |

Before every UWP build the scripts overwrite the `WSATestCertificate.pfx` that Unity generates (it carries the
`_Imaginary_` company name) with this one and force `Publisher="CN=TNT ENTERTAINMENT Inc"` into the manifest, so
MSBuild signs the package and produces the matching `.cer`. The Surface RT packer signs with the same `.pfx` and then
reads the thumbprint back out of `AppxSignature.p7x` — it fails the build if it does not match.

Because the publisher is identical everywhere, every package gets the same family name,
`BadPiggiesRebooted_x0xr9mv5edyrp`, and any `.cer` from any folder trusts all of them. A package signed with a
different certificate becomes a separate app with a separate save folder — which is exactly why the save scripts
look the folder up by prefix.

The Surface RT/2 folder ships the certificate as `BPRE_UWP.cer` instead of
`Bad_PiggiesRebooted_<version>_ARM32_TegraPatch.cer`. That name is intentional: it matches the `BPRE_UWP.cer` asset
of the published release, and `Install.ps1` accepts any single `*.cer` in its folder.

## Releases

Prebuilt packages are attached to the GitHub releases
(<https://github.com/Max-RM/BPRE_WinARM32/releases>). The current Windows port release is tagged `1.0.0.0_0`; its
assets:

| Asset | For |
|---|---|
| `BPRE-Delta-Epsilon-win64.zip`, `-win32.zip` | the standalone game, unpack and run |
| `BPRE-Delta-Epsilon-uwp-x64.zip`, `-uwp-x86.zip` | UWP installer + certificate + VCLibs |
| `BPRE-Delta-Epsilon-uwp-ARM32.zip` | Windows 10 Mobile, Windows on ARM, Windows 10 ARM32 15035 |
| `BPRE-Delta-Epsilon-uwp-ARM32-TegraPatch.zip` | Surface RT / Surface 2 on Windows 10 ARM32 15035 |
| `Bad_PiggiesRebooted_*_ARM32*.appx`, `BPRE_UWP.cer` | loose packages, for CMD Injector / Device Portal |

The ARM32 packages are not byte-reproducible: zipping sets fresh timestamps, so every rebuild produces a different
`sha256` even when the content is identical.

## Port notes

- `Assets\Editor\BpReBuild.cs` contains the batch build entry points (`BuildWin64`, `BuildWin32`,
  `BuildWSA`, `BuildWSA64`, `BuildWSA86`) used by the scripts.
- MSBuild does not copy the Unity `Data` folder next to the loose UWP player, so the build scripts
  sync it manually. A `global-metadata.dat` that does not match `GameAssembly.dll` crashes the
  player at startup, so never mix folders from different builds.
- Every build starts from an empty output folder (`Reset-OutputFolder`): Unity 2018 appends player
  data to an existing build, which corrupts `resources.assets` ("Position out of bounds!" at startup).
  For the same reason the scripts clear Unity's `Library\PlayerDataCache` before every build — the
  cached player data is not always valid after a build-target switch (for example x64 → x86).
- `Assets\Scripts\Assembly-CSharp\INInitializer.cs` swaps the innovation splash for the ARM32
  artwork when the process is 32-bit (`System.IntPtr.Size == 4`). The texture is loaded from
  `Assets\Resources\innovation\Innovation_Background_WinARM32.png`; it must stay **2048×1024 and
  compressed**, otherwise Unity 2018 Standalone writes a broken `resources.assets` entry and the
  Win64 player crashes with `The file 'resources.assets' is corrupted! [Position out of bounds!]`.
- The Windows/UWP system splash (the screen shown before Unity starts) is taken from
  `BuildSupport\UWP\SplashScreen.scale-200.png` and its background colour is patched in the
  generated manifests by the build scripts.
- `Assets\Scripts\Assembly-CSharp\DeviceDependency.cs` gates objects by device family and is required by the
  ARM32 player — do not remove it.
- The UWP package is replaced in place on already registered devices, so a rebuild does not need
  re-registration (a loose package is registered by folder path, a sideloaded one by version).
- The Surface RT packer (`scripts\Build-UWP-ARM32-L9D3D11-RT-Fix.ps1`) unpacks the ARM32 `.appx`, points the
  `d3d11*.dll` / `*_APP.dll` imports of the player at `l9d3d11.dll` (see `patch-pe.ps1`), adds the shim, the shader
  table, the app-local VC++ ARM runtime and the four loose-install files, drops the `Microsoft.VCLibs.140.00`
  dependency from `AppxManifest.xml`, then packs and signs. It refuses to work on a `UnityPlayer.dll` it has not been
  validated against — the shim's capability patches depend on that exact build.
- The shim and the shader table come from `l9d3d11_Fix\`; that folder also documents the provenance of every binary
  and how to rebuild or update the LLVM toolchain.
- Everything that runs on the device (installers, save scripts, batch files) is ASCII-only and avoids
  `Add-Type`, `New-Object` and .NET method calls: Windows 10 ARM32 build 15035 runs PowerShell in Constrained
  Language Mode and its console has no UTF-8 support. Keep new scripts that way.
- Entry names inside the `.appx` are OPC percent-encoded (`Bad%20PiggiesRebooted.exe`,
  `unity%20default%20resources`). `makeappx`, the AppX runtime and the Device Portal decode them; 7-Zip, Explorer and
  "Extract All" do not, which is what `Install-Loose.bat` repairs.
- A loose UWP package can be registered for testing with:
  ```powershell
  Add-AppxPackage -Register "Builds\WSA64\build\bin\x64\Release\AppxManifest.xml"
  ```
  Registering by folder path means a rebuild is picked up without re-registration.

## Repository layout

```
Assets\              Unity project assets and C# sources
BuildSupport\        extra files injected into the builds
  UWP\               UWP splash screen, icons and the signing certificate (BPRE_UWP.pfx / .cer)
  UWP\Install\       Install.bat / Install.ps1 / Open-Saves.bat / README.txt published next to the .appx
  Win\               Open-Saves.bat template for the standalone builds
l9d3d11_Fix\         Direct3D 9_1 shim for Surface RT/2: sources, prebuilt dll, shader table,
                     VC++ ARM runtime, LLVM toolchain, packer helpers and documentation
Packages\            Unity package manifest
ProjectSettings\     Unity project settings (version, player settings, scenes)
scripts\             PowerShell build / clean / package scripts
```

## Credits

- **MaxRM (TNT ENTERTAINMENT Inc)** — Windows port lead: the Unity 2018.4 downgrade and project setup, the ARM32 /
  UWP toolchain and packaging, the installers and save helpers, the Surface RT / Surface 2 work, release builds and
  documentation.
- **DeepSeek V4.1 Flash Free** — co-author of the port: code, build and packaging scripts, the Direct3D 9_1 layer
  work, installers and documentation (October 2026).
- [Max-RM/Win32Craft_ARM](https://github.com/Max-RM/Win32Craft_ARM) — the Direct3D 11 → 9_1 approach behind
  `l9d3d11`, first designed and proven there on a Surface 2.
- [Miuna](https://github.com/miu-na) for all the prior modding effort on Bad Piggies.
- [Goggs](https://github.com/Goggs77) - BP::RE project leader.
- [Anstro Pleuton](https://github.com/anstropleuton) - Some BP::RE features.
- Elderberry Starz - Many BP::RE custom assets.
- SaltedFish (DStuff) - Many BP::RE custom assets.
- [Creato](https://www.youtube.com/channel/UCJjkxlcWgDfMtm1WdIZnUBg) - Some BP::RE custom assets.
- Dartn - Initial decompilation of BPLE as the basis for BP::RE, project coordinator.
- [Egan](https://www.youtube.com/channel/UC71a3L1Lm-aDq5SK8owkBaA) - April fools custom assets.
- [snailmaster42](https://www.youtube.com/channel/UCudLJRGcDBTx9WO8NWoZ36A) - Some BP::RE custom assets.
- [Vas2000](https://www.reddit.com/user/Vas2000) - April fools splash screen, BP::RE custom assets.
- And counting...

## Legal Notice

**This mod is not affiliated with Rovio in any way. Please note that the game Bad Piggies is developed by Rovio.
Most of the unmodified source codes and resources belongs to them.**

The original Bad Piggies :: Rebooted project is ***not*** Open Source: the source code is published in the hope of
software preservation, game longevity and community extension. The same applies to this port.
