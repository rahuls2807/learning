@echo off
setlocal enabledelayedexpansion

echo.
echo ========================================
echo   Launching Worker Booking System
echo ========================================
echo.

echo Starting the application...
echo.
cd /d c:\Users\rsing\source\repos\WorkerBookingSystem

set ASPNETCORE_ENVIRONMENT=Development
dotnet run --launch-profile http
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Application failed to start!
    echo.
    exit /b 1
)
