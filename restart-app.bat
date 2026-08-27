@echo off
setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Restarting Worker Booking System
echo ========================================
echo.

echo Stopping any running app instance...
for /f "skip=2 tokens=2" %%P in ('wmic process where "name='dotnet.exe' and commandline like '%%WorkerBookingSystem%%'" get processid 2^>nul') do (
    taskkill /PID %%P /F >nul 2>&1
)
if !errorlevel! equ 0 (
    echo [OK] App instance stopped
    timeout /t 2 /nobreak
) else (
    echo [INFO] No matching app instance found
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

set ASPNETCORE_ENVIRONMENT=Development
dotnet build "WorkerBookingSystem.csproj" -c Release --nologo
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Build failed before restart.
    echo.
    exit /b 1
)

dotnet run --project "WorkerBookingSystem.csproj" --no-build --launch-profile http --urls http://localhost:5156
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Application failed to start!
    echo.
    exit /b 1
)
