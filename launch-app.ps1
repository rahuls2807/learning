# Set execution policy for this process to allow script execution
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Launching Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Starting the application...`n" -ForegroundColor Green
Push-Location $appPath

$env:ASPNETCORE_ENVIRONMENT = "Development"

$launchArgs = @("run", "--launch-profile", "http")
& dotnet @launchArgs
$exitCode = $LASTEXITCODE

Pop-Location

if ($exitCode -ne 0) {
    Write-Host "`n[ERROR] Application failed to start!`n" -ForegroundColor Red
    exit $exitCode
}

exit $exitCode
