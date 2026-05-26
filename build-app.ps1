# Set execution policy for this process to allow script execution
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Building Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Stopping any running instances..." -ForegroundColor Yellow
$dotnetProcs = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($dotnetProcs) {
    $dotnetProcs | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] Process terminated" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "[INFO] No running process found" -ForegroundColor Gray
}

Push-Location $appPath

Write-Host "`nCleaning previous build..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBuilding the project...`n" -ForegroundColor Green

dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Build completed successfully!`n" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Build failed with errors!`n" -ForegroundColor Red
}

Pop-Location
