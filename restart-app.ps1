# Stop any running dotnet processes
Write-Host "Stopping the application..." -ForegroundColor Yellow
Stop-Process -Name dotnet -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Start the app
Write-Host "Starting the application..." -ForegroundColor Green
Set-Location "c:\Users\rsing\source\repos\WorkerBookingSystem"
dotnet run
