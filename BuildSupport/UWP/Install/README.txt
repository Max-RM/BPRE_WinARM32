========================================================================
 Bad Piggies :: Rebooted - UWP package (sideload)
========================================================================

This folder contains everything needed to install the game on a Windows 10
or Windows 11 device, including Windows 10 Mobile and Windows on ARM.

  Bad PiggiesRebooted_1.0.0.0_<arch>.appx   the game itself
  Bad PiggiesRebooted_1.0.0.0_<arch>.cer    the signing certificate
  Dependencies\Microsoft.VCLibs.<arch>.14.00.appx   required runtime
  Install.bat / Install.ps1                 automatic installer (desktop)
  README.txt                                this file

The package architecture must match the device:

  BPRE_UWP-x64     ->  Windows 10/11 on x64 (Intel/AMD)
  BPRE_UWP-x86     ->  32-bit Windows 10/11 (and a test target for the other
                      32-bit builds; most devices do not need it)
  BPRE_UWP-ARM32   ->  Windows 10/11 on ARM (ARM64 devices run it through
                      ARM32 emulation) and Windows 10 Mobile

Never mix files from different folders.

Every package is signed by the same certificate, CN=TNT ENTERTAINMENT Inc.

Minimum OS version: 10.0.15035 (Windows 10 Mobile build 15035+).
Windows 10 Mobile 10.0.15063 (Creators Update) or newer is recommended.
The ARM32 package also works on the leaked Windows 10 ARM32 desktop build
15035 and on Windows 10/11 ARM64 through ARM32 backward compatibility.


------------------------------------------------------------------------
 1. DESKTOP WINDOWS 10 / 11 (x64 or ARM64)
------------------------------------------------------------------------

The easy way:

  1. Copy the whole folder to the device (keep all files together).
  2. Double-click Install.bat.
  3. Confirm the administrator prompt (it is needed to trust the
     certificate).
  4. Wait for "Installed: ..." and answer the question about starting
     the game.

Install.bat does the same as the manual steps below: it imports the
.cer into Local Machine -> Trusted People, installs the VCLibs
dependency and then the game.

Manual installation (if you prefer PowerShell). Run these in an
administrator PowerShell window opened in this folder:

  # 1. Trust the certificate:
  certutil -addstore -f TrustedPeople (Get-ChildItem *.cer).FullName

  # 2. Install the dependency first:
  Add-AppxPackage -Path (Get-ChildItem "Dependencies\*.appx").FullName

  # 3. Install the game:
  Add-AppxPackage -Path (Get-ChildItem *.appx).FullName

The game then appears in the Start menu as "Bad Piggies::Rebooted".

If the certificate was imported into the wrong store, Windows refuses the
package with 0x800B0109 (untrusted) or 0x800B010A (chain). Remove the
certificate and repeat step 1.

Windows 11 requires the "Sideload apps" or "Developer mode" option:
Settings -> System -> For developers. On Windows 10 the same setting is
under Settings -> Update & security -> For developers.


------------------------------------------------------------------------
 2. WINDOWS 10 MOBILE (Lumia and similar phones)
------------------------------------------------------------------------

The phone cannot run Install.bat, so the certificate and the package are
installed separately. Use the ARM32 package (BPRE_UWP-ARM32 folder).

Prepare the phone:

  a. Settings -> Update & security -> For developers:
     enable "Developer mode" (or "Sideload apps").
  b. Make sure the phone has at least ~500 MB of free storage.
  c. Copy the .appx, the .cer and the Dependencies folder to the phone
     (USB mass storage, OneDrive or a microSD card).

Trust the certificate:

  1. On the phone open the File Explorer / OneDrive and tap the .cer file.
  2. Accept the warning and install it. If the phone asks for a store,
     choose "Trusted People" (some builds only offer "Trusted Root
     Certification Authorities" - that also works).
  3. If tapping the .cer does nothing, use the certificate import
     feature of CMD Injector / Interop Tools instead.

Install the game with CMD Injector:

  1. Install CMD Injector on the phone and enable its "full file system
     access" / interop unlock as described by that tool.
  2. Deploy the dependency first:
     Dependencies\Microsoft.VCLibs.arm.14.00.appx
  3. Deploy the game:
     Bad PiggiesRebooted_1.0.0.0_ARM.appx
  4. Launch "Bad Piggies::Rebooted" from the app list.

Alternative without CMD Injector - Device Portal:

  1. On the phone: Settings -> Update & security -> For developers ->
     enable Device Portal (and note the shown IP address / credentials).
  2. On a PC open http://<phone-ip>:80 in a browser and sign in.
  3. Apps -> Deploy: first upload the VCLibs .appx, then the game .appx.
  4. Start the game from the phone's app list.

Note: the ARM32 package needs the Microsoft.VCLibs.140.00 runtime of
version 14.0.33519.0 or newer. Always deploy the copy from the
Dependencies folder before the game.


------------------------------------------------------------------------
 3. WINDOWS 10 ARM32 DESKTOP BUILD 15035
------------------------------------------------------------------------

This early 32-bit ARM desktop build has PowerShell in Constrained Language
Mode, so older installers that used Add-Type or System.IO.Compression fail
with "Cannot add type. Definition of new types is not supported in this
language mode." The Install.ps1 in this folder avoids those calls and works
there. If Install.bat still refuses to start, open an administrator
PowerShell window in this folder and run:

  powershell -ExecutionPolicy Bypass -File .\Install.ps1

The script imports the .cer, installs the VCLibs dependency and then the
ARM32 package. You can also install everything manually:

  certutil -addstore -f TrustedPeople Bad*ARM.cer
  Add-AppxPackage -Path .\Dependencies\Microsoft.VCLibs.arm.14.00.appx
  Add-AppxPackage -Path .\Bad*ARM.appx


------------------------------------------------------------------------
 4. TROUBLESHOOTING
------------------------------------------------------------------------

0x80073CF3  A dependency is missing or too old. Install the .appx from
            the Dependencies folder first (or keep that folder next to
            the game package and use Install.bat).
0x80073CF0  The package file is damaged. Copy/download it again.
0x80073CFB  This or a newer version is already installed. Remove the old
            one (Settings -> Apps, or Remove-AppxPackage) and retry.
0x80073D02  The package is in use. Close the game and retry.
0x800B0109  The certificate is not trusted. Import the .cer from this
            folder into Trusted People (as administrator) and retry.
0x800B010A  The .cer does not belong to this .appx. Use the .cer from the
            same folder as the package.
0x80073CF9  Installation of a loose/registered package failed. Remove the
            previous registration and try again.

The game needs DirectX 11 feature level 10.0 or higher. Devices that only
offer Direct3D 9.x (for example older ARM tablets) fall back to software
rendering and will be slow - this is a limitation of the Unity 2018.4
player, not of this package.

Logs: %LOCALAPPDATA%\Packages\BadPiggiesRebooted_x0xr9mv5edyrp\TempState\
(UnityPlayer.log) on desktop; on the phone the same file is inside the
package's local state folder.


------------------------------------------------------------------------
 Ported by MaxRM, TNT ENTERTAINMENT Inc, using DeepSeek V4.1 Flash Free.
 Bad Piggies is a trademark of Rovio Entertainment. This port is a
 non-commercial preservation project and is not affiliated with Rovio.
========================================================================
