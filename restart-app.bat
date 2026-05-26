@echo off
setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Restarting Worker Booking System
echo ========================================
echo.

echo Stopping any running instances...
taskkill /F /IM dotnet.exe >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] Process terminated
    timeout /t 2 /nobreak
) else (
    echo [INFO] No running dotnet process found
)

echo.
echo Starting the application...
echo.
cd /d c:\Users\rsing\source\repos\WorkerBookingSystem

if not exist bin\nul (
    echo Cleaning build artifacts...
    rmdir /s /q bin >nul 2>&1
    rmdir /s /q obj >nul 2>&1
)

dotnet run
pause
