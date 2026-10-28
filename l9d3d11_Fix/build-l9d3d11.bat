@echo off
rem Rebuild l9d3d11.dll from src\ with the compiler bundled in llvm-rt\.
rem Double-click this file, or run it from a terminal. Pass -Force to rebuild anyway.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-l9d3d11.ps1" %*
echo.
pause
