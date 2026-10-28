@echo off
rem Opens the game's save folder in a new Explorer window, so progress can be
rem backed up by hand before the game is moved, updated or reinstalled.
rem
rem This file is a template. Publish-Standalone replaces @SAVES@ with the folder
rem Application.persistentDataPath uses, built from companyName and productName
rem in ProjectSettings.asset:
rem   %USERPROFILE%\AppData\LocalLow\<company>\<product>
rem The copies in Publish\BPRE_Win32 and Publish\BPRE_Win64 are the finished scripts.
setlocal
set "SAVES=@SAVES@"
if exist "%SAVES%\" goto open

echo.
echo The save folder does not exist yet:
echo   %SAVES%
echo.
echo It is created the first time the game is started.
echo Opening %USERPROFILE%\AppData\LocalLow instead.
echo.
pause
start "" explorer.exe "%USERPROFILE%\AppData\LocalLow"
exit /b 0

:open
start "" explorer.exe "%SAVES%"
