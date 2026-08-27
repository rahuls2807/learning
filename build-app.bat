@echo off
setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Building Worker Booking System
echo ========================================
echo.

echo Stopping any running instance of this app...
for /f "skip=2 tokens=2" %%P in ('wmic process where "name='dotnet.exe' and commandline like '%%WorkerBookingSystem%%'" get processid 2^>nul') do (
    taskkill /PID %%P /F >nul 2>&1
)
if !errorlevel! equ 0 (
    echo [OK] App instance stopped
    timeout /t 2 /nobreak
) else (
    echo [INFO] No matching app instance found
)

cd /d c:\Users\rsing\source\repos\WorkerBookingSystem

echo.
echo Cleaning previous build...
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1

echo.
echo Building the project in Release mode...
echo.

dotnet build "WorkerBookingSystem.csproj" -c Release --nologo

if !errorlevel! equ 0 (
    echo.
    echo [SUCCESS] Build completed successfully!
    echo.
) else (
    echo.
    echo [ERROR] Build failed with errors!
    echo.
    exit /b 1
)
