@echo off
echo Stopping the application...
taskkill /F /IM dotnet.exe >nul 2>&1
timeout /t 2 /nobreak
echo Starting the application...
cd /d c:\Users\rsing\source\repos\WorkerBookingSystem
dotnet run
pause
