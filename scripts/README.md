# Build scripts

PowerShell scripts for building **Bad Piggies :: Rebooted** (Windows port) without opening the Unity editor.

## Requirements

| Component | Version / notes |
|---|---|
| Unity | **2018.4.36f1** (project version) with the **Windows Build Support** and **UWP Build Support** modules |
| Visual Studio | 2022 with **Universal Windows Platform development** workload **and the MSVC v143 ARM build tools** (`Microsoft.VisualStudio.Component.VC.Tools.ARM`) |
| Windows SDK | **10.0.19041.0** |
| MSVC toolset | **MSVC v143** — any toolset that ships ARM32 (all current ones do once the ARM build tools component is installed) |
| PowerShell | 5.1 (built into Windows 10/11) |
| 7-Zip | optional, used by `Package-Builds.ps1` when available |

Unity 2018.4.36f1 downloads:

- archive page: <https://unity.com/releases/editor/archive>
- direct installer: <https://download.unity3d.com/download_unity/6cd387d23174/Windows64EditorInstaller/UnitySetup64-2018.4.36f1.exe>
- backup mirror: <https://t.me/TNT_ENTERTAINMENT_inc/165114>

> **ARM32 note.** The ARM32 target needs the *ARM build tools* component in Visual Studio. The
> "Desktop development with C++" and "Universal Windows Platform development" workloads do not
> install it, and without it IL2CPP stops with
> `architecture ARMv7 is not supported by this Visual Studio version`. Add
> **MSVC v143 - VS 2022 C++ ARM build tools** from the Visual Studio Installer (Individual
> components → Compilers, build tools and runtimes), or from the command line:
>
> ```powershell
> & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\setup.exe" modify `
>     --installPath "C:\Program Files\Microsoft Visual Studio\2022\Community" `
>     --add Microsoft.VisualStudio.Component.VC.Tools.ARM --quiet --norestart
> ```
>
> The component installs `bin\Hostx64\arm` and `lib\arm` for the MSVC toolsets on the machine.
> IL2CPP resolves the toolchain through the Visual Studio *default toolset*
> (`VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt`); if that version has no
> `bin\Hostx64\arm`, point the file at one that does.

Unity and MSBuild are auto-detected. Override with `-UnityPath` / `-MSBuildPath`,
or the `UNITY_EDITOR` / `MSBUILD_EXE` environment variables.

## Prerequisite checks

Every build starts with `Assert-BuildPrerequisites` (in `Build-Common.ps1`), which verifies:

- Unity is installed and is the expected version (`ProjectSettings\ProjectVersion.txt`),
- the Unity installation has the `MetroSupport` playback engine (UWP builds),
- MSBuild / Visual Studio 2022 is present,
- the **Universal Windows Platform development** workload is installed,
- the **Windows 10 SDK 10.0.19041.0** is installed,
- the **ARM build tools** are installed and the default MSVC toolset ships `bin\Hostx64\arm` (ARM32 build).

Missing Visual Studio components are offered for installation in a dialog: answering **Yes** starts

```
setup.exe modify --installPath "<VS path>" --add <component> --quiet --norestart
```

with administrator rights and waits until the component appears. Components that cannot be installed
automatically (Unity itself, the Unity UWP module) produce a dialog with the download links above.

If a build fails, the error is printed, shown in a dialog and the script waits for a key press instead of
closing the window immediately (`-NoPause` disables the dialog and the wait, e.g. for CI). `Build-All.ps1` passes
`-NoPause` to the child scripts and pauses once at the end.

## Scripts

| Script | Purpose |
|---|---|
| `Build-Win64.ps1` | Windows x64 standalone (Mono) → `Builds\Win64` → `Publish\Win64` |
| `Build-Win32.ps1` | Windows x86 standalone (Mono) → `Builds\Win32` → `Publish\Win32`; also the local test target for the 32-bit (ARM32) code path |
| `Build-UwpX64.ps1` | UWP x64 (IL2CPP) → `Builds\WSA64` → `Publish\UWP-x64` |
| `Build-UwpArm32.ps1` | UWP ARM32 (IL2CPP) → `Builds\WSA` → `Publish\UWP-ARM32` |
| `Build-All.ps1` | All four targets, in order ARM32 → x64 → Win32 → Win64 |
| `Clean-BuildOutput.ps1` | Removes builds, `Publish`, IL2CPP caches and logs; `-Deep` also removes `Library` |
| `Package-Builds.ps1` | Packs the `Publish` folder into `Archives\*.zip` |
| `Build-Common.ps1` | Shared helpers (paths, prerequisite checks, dialogs, Unity/MSBuild detection, Data sync, UWP splash, publishing) |

## Usage

```powershell
# one target
.\scripts\Build-UwpX64.ps1

# everything
.\scripts\Build-All.ps1

# only some targets
.\scripts\Build-All.ps1 -Targets uwp-arm32,uwp-x64

# 32-bit standalone (also used to check the ARM32 splash locally)
.\scripts\Build-Win32.ps1

# build without copying the result into Publish
.\scripts\Build-UwpArm32.ps1 -NoPublish

# build without the "press any key" pause on errors
.\scripts\Build-All.ps1 -NoPause

# clean build outputs (keeps the Unity Library cache)
.\scripts\Clean-BuildOutput.ps1

# full clean, including Library (next build reimports everything)
.\scripts\Clean-BuildOutput.ps1 -Deep

# pack the published outputs
.\scripts\Package-Builds.ps1
```

## What the UWP builds do

1. Check the required components (see above).
2. `Unity.exe -batchmode -executeMethod BpReBuild.BuildWSA64` (or `BuildWSA` for ARM32).
3. Patch the UWP splash image (`BuildSupport\UWP\SplashScreen.scale-200.png`)
   and its background colour in `Package.appxmanifest`.
4. Build the generated solution with MSBuild (`x64` or `ARM`, Release).
5. Copy `Bad PiggiesRebooted\Data` next to the loose player (MSBuild does **not**
   do this, and a mismatch between `global-metadata.dat` and `GameAssembly.dll`
   crashes the player at startup).
6. Patch the splash in the loose output (`AppxManifest.xml`).
7. Copy the package into `Publish\` together with:
   - `Install.bat` + `Install.ps1` — automatic installer (UAC, certificate trust, VCLibs, game),
   - `README.txt` — installation instructions for desktop Windows, Windows on ARM and Windows 10 Mobile,
   - `Dependencies\Microsoft.VCLibs.<arch>.14.00.appx` — the runtime dependency required by the manifest.

## Outputs

```
Builds\Win64\BadPiggiesRebooted.exe
Builds\Win32\BadPiggiesRebooted.exe
Builds\WSA64\build\bin\x64\Release\            (loose UWP x64 player)
Builds\WSA64\AppPackages\...\*_x64_Test\*.appx
Builds\WSA\build\bin\ARM\Release\              (UWP ARM32 player)
Builds\WSA\AppPackages\...\*_ARM_Test\*.appx

Publish\Win64\                                 full game folder
Publish\Win32\                                 same, 32-bit player
Publish\UWP-x64\                               *.appx, *.cer, Install.bat, Install.ps1, README.txt, Dependencies\
Publish\UWP-ARM32\                             same, ARM32 package

Archives\BPRE-<version>-*.zip
```

## Notes

- Close the Unity editor before running a batch build; the scripts refuse to run
  while `Unity.exe` is active because the project folder would be locked.
- Every build removes its old output folder first (`Reset-OutputFolder`). Unity 2018 appends
  player data to an existing folder, and a second build into the same folder corrupts
  `resources.assets` (the game then crashes with `Position out of bounds!`).
- Switching between Standalone (Win64) and UWP forces Unity to reimport scripts,
  so a full four-target run takes noticeably longer than a single target.
- The 32-bit standalone (`Build-Win32.ps1`) uses the same 32-bit code path as the ARM32
  package (`System.IntPtr.Size == 4`, port splash, 16-bit-safe asset layout), so it is the
  quickest way to test ARM32-specific behaviour on a normal PC.
- The UWP splash is taken from `BuildSupport\UWP\SplashScreen.scale-200.png`
  (1240×600). Replace that PNG to change the Windows splash screen.
- The in-game innovation splash for the 32-bit builds (UWP ARM32 and Windows x86) is loaded from
  `Assets\Resources\innovation\Innovation_Background_WinARM32.png` — the x64 UWP and
  Win64 builds keep the original `Innovation_Background.png`. Keep that PNG at
  **2048×1024 (power of two) and compressed**: Unity 2018 Standalone writes a broken
  `resources.assets` entry for an uncompressed non-power-of-two texture, and the player
  then fails with `The file 'resources.assets' is corrupted! [Position out of bounds!]`.
- Switching between build targets (Standalone x86 ↔ x64, UWP x64 ↔ ARM32) makes Unity reuse the
  serialized player data in `Library\PlayerDataCache`, and the reused copy is not always valid for
  the new target (a 32-bit player built that way dies at startup with
  `resources.assets is corrupted!`). Every build therefore clears that cache first, which costs a
  few seconds and no reimport.
- A loose UWP package can be registered for testing with:
  ```powershell
  Add-AppxPackage -Register "Builds\WSA64\build\bin\x64\Release\AppxManifest.xml"
  ```
  Registering by folder path means a rebuild is picked up without re-registration.
