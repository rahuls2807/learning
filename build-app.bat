@echo off
setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Building Worker Booking System
echo ========================================
echo.

echo Stopping any running instances...
taskkill /F /IM dotnet.exe >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] Process terminated
    timeout /t 2 /nobreak
) else (
    echo [INFO] No running process found
)

cd /d c:\Users\rsing\source\repos\WorkerBookingSystem

echo.
echo Cleaning previous build...
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1

echo.
echo Building the project...
echo.

dotnet build

if !errorlevel! equ 0 (
    echo.
    echo [SUCCESS] Build completed successfully!
    echo.
) else (
    echo.
    echo [ERROR] Build failed with errors!
    echo.
)

pause
