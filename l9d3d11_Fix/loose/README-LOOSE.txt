======================================================================
 Bad Piggies :: Rebooted - install from this unpacked folder
 (Surface RT / Surface 2, Windows 10 ARM32 build 15035)
======================================================================

This folder is the game package unpacked into normal files. Windows can run
it from here without the signed .appx and without trusting any certificate.
That is what Install-Loose.bat does; the game itself is exactly the same.

WHY USE THIS INSTEAD OF THE .APPX
  The signed .appx needs the signing certificate in Trusted People before it
  installs. This variant needs no certificate at all, only Developer Mode.
  It is also the only way to look inside the package and see what was added.

BEFORE YOU START
  * Developer Mode must be on:
      Settings -> Update & security -> For developers -> Developer mode
  * Keep the whole folder together on the local disk (C:\BadPiggies is fine).
    Do not put it in the root of a drive, in Program Files, on a read-only
    medium or on a network share: Windows cannot register an app from there.
  * Nothing else has to be installed - the Visual C++ runtime and the
    Direct3D 9_1 shim (l9d3d11.dll) are already in this folder.

HOW TO INSTALL
  1. Double-click  Install-Loose.bat  (or right-click Install-Loose.ps1 and
     choose "Run with PowerShell").
  2. Wait for "Registered". Start "Bad Piggies::Rebooted" from the Start menu.
  3. Leave the folder where it is. Windows runs the game from this folder;
     moving, renaming or deleting it breaks the installation.

  Unpacked with 7-Zip, Explorer or "Extract All"? Those tools do not decode
  the names inside an .appx, so the game exe arrives as
  "Bad%20PiggiesRebooted.exe" and one data folder as
  "unity%20default%20resources". Install-Loose.bat renames them back to
  "Bad PiggiesRebooted.exe" and "unity default resources" before registering,
  so nothing has to be renamed by hand. (makeappx unpack already decodes them.)

IF THE GAME IS ALREADY INSTALLED
  Install-Loose.bat stops and says so. Windows cannot replace or update an
  installed copy from an unpacked folder (0x80073CFB / 0x80073D06), so the
  old copy has to be removed by hand first. Removing it also deletes its
  save data, so back the saves up first:
    1. Run  Open-Saves.bat  - it opens the save folder in Explorer.
    2. Copy that whole folder to the Desktop, a USB stick or a network share.
    3. Remove the app: Settings -> Apps (Apps & features) ->
       Bad Piggies::Rebooted -> Uninstall.
    4. Run Install-Loose.bat again.

  The save folder is:
    %LOCALAPPDATA%\Packages\BadPiggiesRebooted_<hash>\LocalState
  (Progress.dat, Settings.xml, playerprefs.dat and contraptionsB\).

  <hash> is part of the package family name and depends on the certificate
  the package was signed with: x0xr9mv5edyrp for this build, something else
  (for example ...hj) for another one. Open-Saves.bat does not rely on the
  exact name - it tries the known one first, then any
  BadPiggiesRebooted_* folder under %LOCALAPPDATA%\Packages.

REMOVING THE GAME
  Settings -> Apps -> Bad Piggies::Rebooted -> Uninstall, or:
    Get-AppxPackage BadPiggiesRebooted | Remove-AppxPackage
  Then delete this folder. Back up the saves first - uninstalling removes them.

IF IT DOES NOT START OR LOOKS WRONG
  Send back the file  l9shim.log  from the save folder above. It records the
  GPU, the Direct3D feature level the shim obtained, how many shaders it
  swapped and any error. The regular Unity log is next to it.

  Deployment errors are printed by the script; the message contains an
  ActivityId, and  Get-AppxLog -ActivityID <that id>  shows the full log.
  (Build 15035 runs PowerShell in Constrained Language Mode, where the log
  cannot be written to a file, so the script prints the command instead.)

FILES IN THIS FOLDER THAT ARE NOT PART OF THE GAME
  l9d3d11.dll                        Direct3D 11 -> feature level 9_1 shim
  Data\bp\l9_replacements.bin        level-9 shader replacements
  vcruntime140.dll, msvcp140*.dll, vccorlib140.dll, concrt140.dll
                                     Visual C++ runtime, app-local
  Install-Loose.bat / .ps1           registers this folder as an app
  Open-Saves.bat                     opens the save folder for a backup
  README-LOOSE.txt                   this file

  Everything else (AppxManifest.xml, Assets\, Data\, resources.pri,
  UnityPlayer.dll, GameAssembly.dll and the game .exe - its name follows the
  Unity product, BadPiggiesRebooted.exe or Bad PiggiesRebooted.exe) is the
  game.  UnityPlayer.dll / GameAssembly.dll / the game .exe have their
  d3d11 and *_APP runtime imports repointed; nothing else in them changed.

Note: installing the signed .appx later replaces this registration, and
installing this folder over a signed install is refused for the reason above.
The two variants share the same identity and save folder.

Ported by MaxRM, TNT ENTERTAINMENT Inc.
Bad Piggies is a trademark of Rovio Entertainment. This port is a
non-commercial preservation project and is not affiliated with Rovio.
======================================================================
