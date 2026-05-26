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

dotnet run
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Application failed to start!
    echo.
    pause
)
