# Set execution policy for this process to allow script execution
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Launching Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Starting the application...`n" -ForegroundColor Green
Push-Location $appPath

dotnet run
Pop-Location
