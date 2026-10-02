@echo off
setlocal
cd /d "%~dp0"

echo Restarting Worker Booking System...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0restart-app.ps1"
if errorlevel 1 (
    echo.
    echo Restart failed. Review the error above or check the logs in %%LOCALAPPDATA%%\WorkerBookingSystem.
    pause
    exit /b 1
)
exit /b 0
