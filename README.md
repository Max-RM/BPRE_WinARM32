# Bad Piggies :: Rebooted Edition — Windows ARM32 port

A **Windows port** of [Bad Piggies :: Rebooted Edition](https://github.com/Goggs77/bp-rebooted), a community-driven
modification of Bad Piggies.

The original mod ships as an Android build that only runs through emulation on Windows. This repository contains a
self-contained Unity project that builds the very same game natively for Windows, including the **ARM32 target that
never existed before this port**.

**Ported by MaxRM, TNT ENTERTAINMENT Inc, using DeepSeek V4.1 Flash Free — October 2026.**

## Supported systems

| Target | System | Backend | Status |
|---|---|---|---|
| **UWP ARM32** | Windows 10 Mobile 10.0.15063+ (Tested on HP Elite x3), Windows 10 ARM32 15035 (The leaked 32-bit desktop ARM build) and Windows 10/11 ARM64 (ARM64 devices run it through ARM32 backward compatibility) | IL2CPP | main target of this port |
| UWP x64 | Windows 10/11 on x64 (desktop mode) | IL2CPP | works |
| UWP x86 | 32-bit Windows 10/11 and the x86 test target | IL2CPP | works; useful as an extra 32-bit test build |
| Windows x64 | Windows 7 SP1 / 8 / 8.1 / 10 / 11 (standalone, no sideloading) | Mono | works |
| Windows x86 | 32-bit Windows 7 SP1 / 8 / 8.1 / 10 / 11 (standalone) | Mono | works; also the local test target for the 32-bit code path |

The 32-bit builds (UWP ARM32 and Windows x86)
show a dedicated splash screen (`Assets/Resources/innovation/Innovation_Background_WinARM32.png`) with
the porting credits; the x64 builds keep the original splash. All builds use the same Unity 2018.4 IL2CPP/Mono player code, so gameplay is
identical — only the packaging and the rendering backend differ.

### Surface RT and Surface 2

Surface RT and Surface 2 are **not playable** with this build: their GPU/driver combination cannot run
the Unity player correctly. External compatibility work is being developed separately in
[Max-RM/Win32Craft_ARM](https://github.com/Max-RM/Win32Craft_ARM). It has already been proven to work
in testing, but the fix is not published yet.

### Architecture of the port

| Part | Notes |
|---|---|
| Unity project | Downgraded from 2021.3 to **2018.4.36f1** (the last version that still targets Windows 10 Mobile ARM32) |
| UWP ARM32 | IL2CPP, MSVC ARM toolset, packaged as `.appx` with the ARM32 splash patch applied at runtime |
| UWP x64 | IL2CPP, packaged as `.appx` (desktop devices) |
| UWP x86 | IL2CPP, 32-bit package (test target for the 32-bit code path) |
| Win64 | Mono standalone player, no package or certificate needed |
| Win32 | 32-bit Mono standalone player; shares the 32-bit code path (`IntPtr.Size == 4`, ARM32 splash) with the ARM package |
| UWP packaging | `Package.appxmanifest` + MSIX-style `.appx`, signed with a self-signed certificate (`*.cer`) |

## Requirements

Building **any** target requires:

| Component | Version / notes |
|---|---|
| Unity | **2018.4.36f1** — the project was downgraded from 2021.3, newer editors will not upgrade it cleanly |
| Visual Studio | 2022 with the **Universal Windows Platform development** workload **and the MSVC v143 ARM build tools** (`Microsoft.VisualStudio.Component.VC.Tools.ARM`) |
| Windows SDK | **10.0.19041.0** |
| MSVC toolset | **MSVC v143** — any version that ships ARM32 (see the note below) |
| PowerShell | 5.1 (bundled with Windows 10/11) |
| 7-Zip | optional, only used for packing archives |

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
# UWP ARM32 (main target)  -> Builds\WSA      + Publish\BPRE_UWP-ARM32
.\scripts\Build-UWP-ARM32.ps1

# UWP x64                  -> Builds\WSA64    + Publish\BPRE_UWP-x64
.\scripts\Build-UWP-x64.ps1

# UWP x86 (32-bit test target) -> Builds\WSA86 + Publish\BPRE_UWP-x86
.\scripts\Build-UWP-x86.ps1

# Windows x64 standalone   -> Builds\Win64    + Publish\BPRE_Win64
.\scripts\Build-Win64.ps1

# Windows x86 standalone   -> Builds\Win32    + Publish\BPRE_Win32
.\scripts\Build-Win32.ps1

# everything, in one go
.\scripts\Build-All.ps1
```

Unity and MSBuild are auto-detected. Override them with `-UnityPath` / `-MSBuildPath` or the
`UNITY_EDITOR` / `MSBUILD_EXE` environment variables. `-NoPause` skips the "press any key" wait (useful for CI).

Maintenance helpers:

```powershell
# remove build outputs, Publish, IL2CPP caches and logs
.\scripts\Clean-BuildOutput.ps1
# ... and the Unity Library cache too (next build reimports everything)
.\scripts\Clean-BuildOutput.ps1 -Deep

# zip the Publish folder into Archives
.\scripts\Package-Builds.ps1
```

See [`scripts/README.md`](scripts/README.md) for the details of every script.

## Output layout

```
Builds\                          intermediate build trees (not published, git-ignored)
Publish\
  BPRE_Win64\                    the whole Windows x64 game folder
  BPRE_Win32\                    the same game for 32-bit Windows
  BPRE_UWP-x64\
    Bad PiggiesRebooted_1.0.0.0_x64.appx
    Bad PiggiesRebooted_1.0.0.0_x64.cer
    Dependencies\Microsoft.VCLibs.x64.14.00.appx
    Install.bat / Install.ps1 / README.txt
  BPRE_UWP-x86\                  same set of files, x86 package + VCLibs.x86
  BPRE_UWP-ARM32\                same set of files, ARM32 package + VCLibs.arm
Archives\                        optional zip files created by Package-Builds.ps1
```

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

Every UWP package is signed with the same self-signed certificate, `CN=TNT ENTERTAINMENT Inc`, so the
`.cer` from any of the three UWP folders trusts the x64, x86 and ARM32 packages alike.

The ARM32 package also installs on the leaked **Windows 10 ARM32 desktop build 15035**. That build
runs PowerShell in Constrained Language Mode, so `Install.ps1` deliberately avoids `Add-Type` and
`System.IO.Compression`; the installer reads the version and architecture from the file name instead
of the package manifest. If `Install.bat` still cannot start, run
`powershell -ExecutionPolicy Bypass -File .\Install.ps1` as administrator.

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
- `Assets\DeviceDependency.cs` and the `Win32Craft_ARM` plugin folder are required by the ARM32
  player — do not remove them.
- The UWP package is replaced in place on already registered devices, so a rebuild does not need
  re-registration (a loose package is registered by folder path, a sideloaded one by version).

## Repository layout

```
Assets\          Unity project assets and C# sources
BuildSupport\    extra files injected into the builds
  UWP\           UWP splash screen
  UWP\Install\   Install.bat / Install.ps1 / README.txt published next to the .appx
Packages\        Unity package manifest
ProjectSettings\ Unity project settings (version, player settings, scenes)
scripts\         PowerShell build / clean / package scripts
```

## Credits

- Ported by **MaxRM**, **TNT ENTERTAINMENT Inc**, using **DeepSeek V4.1 Flash Free** (October 2026).
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
