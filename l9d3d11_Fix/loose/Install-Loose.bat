@echo off
rem Installs the unpacked copy of Bad Piggies::Rebooted that sits in this folder.
rem Needs Developer Mode. The real work is done by Install-Loose.ps1.
rem Do NOT put the unpacked package into the root of a drive - see README-LOOSE.txt.
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Loose.ps1" -NoPause
set "RESULT=%ERRORLEVEL%"
echo.
pause
exit /b %RESULT%
