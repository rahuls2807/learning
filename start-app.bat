@echo off
setlocal
cd /d "%~dp0"
echo Starting Worker Booking System...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0manage-app.ps1" -Action Start
if errorlevel 1 (
    echo.
    echo Application is not ready. See the error above and logs under %%LOCALAPPDATA%%\WorkerBookingSystem.
    pause
    exit /b 1
)
exit /b 0
