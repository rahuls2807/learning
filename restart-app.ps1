# Set execution policy for this process to allow script execution
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Restarting Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Stop any running dotnet processes
Write-Host "Stopping any running instances..." -ForegroundColor Yellow
$dotnetProcs = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($dotnetProcs) {
    $dotnetProcs | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] Process terminated" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "[INFO] No running dotnet process found" -ForegroundColor Gray
}

Write-Host "`nStarting the application...`n" -ForegroundColor Green
Push-Location $appPath

# Clean build artifacts if needed
if (Test-Path -Path "bin") {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
}

dotnet run
$exitCode = $LASTEXITCODE

Pop-Location

if ($exitCode -ne 0) {
    Write-Host "`n[ERROR] Application failed to start!`n" -ForegroundColor Red
    Read-Host "Press Enter to close"
}

exit $exitCode
