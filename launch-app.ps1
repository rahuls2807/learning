$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Launching Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Starting the application...`n" -ForegroundColor Green
Set-Location $appPath

dotnet run
