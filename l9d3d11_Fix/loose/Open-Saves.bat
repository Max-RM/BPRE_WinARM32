@echo off
rem Opens the game's save folder in a new Explorer window so it can be backed up
rem by hand before the app is removed or reinstalled.
rem
rem The folder is %LOCALAPPDATA%\Packages\<package family name>\LocalState, and
rem the family name ends with a hash of the signing certificate. The release
rem build is BadPiggiesRebooted_x0xr9mv5edyrp, but a package signed with another
rem certificate gets a different hash (for example ...hj). So the folder is
rem looked up instead of being hard-coded: the known name first, then any
rem BadPiggiesRebooted_* package folder, preferring one that already has saves.
setlocal EnableExtensions
set "PACKAGES=%LOCALAPPDATA%\Packages"
set "KNOWN=%PACKAGES%\BadPiggiesRebooted_x0xr9mv5edyrp"
set "PACKAGE="

if exist "%KNOWN%\LocalState\" set "PACKAGE=%KNOWN%"
if not defined PACKAGE for /d %%D in ("%PACKAGES%\BadPiggiesRebooted_*") do if not defined PACKAGE if exist "%%~D\LocalState\" set "PACKAGE=%%~D"
if not defined PACKAGE if exist "%KNOWN%\" set "PACKAGE=%KNOWN%"
if not defined PACKAGE for /d %%D in ("%PACKAGES%\BadPiggiesRebooted_*") do if not defined PACKAGE set "PACKAGE=%%~D"

if not defined PACKAGE goto no_package

set "SAVES=%PACKAGE%\LocalState"
if exist "%SAVES%\" goto open

echo.
echo The save folder does not exist yet:
echo   %SAVES%
echo.
echo It is created the first time the game is started. Opening the package's
echo own folder instead - its TempState subfolder holds the game logs.
echo.
pause
start "" explorer.exe "%PACKAGE%"
exit /b 0

:no_package
echo.
echo No save folder was found - the game has not been installed or started yet.
echo Opening the folder that contains this script instead.
echo.
set "SAVES=%~dp0."
pause
start "" explorer.exe "%SAVES%"
exit /b 0

:open
start "" explorer.exe "%SAVES%"
