@echo off
setlocal
title Bad Piggies::Rebooted - UWP installer
cd /d "%~dp0"

rem Administrator rights are required to trust the signing certificate.
net session >nul 2>&1
if errorlevel 1 (
    echo.
    echo Administrator rights are required to trust the package certificate.
    echo Starting an elevated copy of this window...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1" -NoPause

echo.
pause
