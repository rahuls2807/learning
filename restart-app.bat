@echo off
set SCRIPT_DIR=%~dp0
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%restart-app.ps1"
if errorlevel 1 (
    echo.
    echo Restart failed. Check restart-app.out.log and restart-app.err.log in this folder.
)
pause
