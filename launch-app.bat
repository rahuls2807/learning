@echo off
setlocal
cd /d "%~dp0"

echo Starting Worker Booking System...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0launch-app.ps1"
if errorlevel 1 (
    echo.
    echo The application did not start. Review the error shown above or run status-app.bat.
    pause
    exit /b 1
)
exit /b 0
