$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Building Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Set-Location $appPath

Write-Host "Cleaning previous build..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBuilding the project...`n" -ForegroundColor Green

dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Build completed successfully!`n" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Build failed with errors!`n" -ForegroundColor Red
}
