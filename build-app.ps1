# Set execution policy for this process to allow script execution
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$appPath = "c:\Users\rsing\source\repos\WorkerBookingSystem"
$projectFile = Join-Path $appPath "WorkerBookingSystem.csproj"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Building Worker Booking System" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Stopping any running instance of this app..." -ForegroundColor Yellow
Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "dotnet.exe" -and
        $_.CommandLine -and
        $_.CommandLine.ToLower().Contains("workerbookingsystem")
    } |
    ForEach-Object {
        Write-Host "Stopping PID $($_.ProcessId)" -ForegroundColor DarkYellow
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }

if (-not $?) {
    Write-Host "[INFO] No matching app instance found" -ForegroundColor Gray
}
else {
    Write-Host "[OK] App instance stopped" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

Push-Location $appPath

Write-Host "`nCleaning previous build..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBuilding the project in Release mode...`n" -ForegroundColor Green

& dotnet build $projectFile -c Release --nologo
$exitCode = $LASTEXITCODE

Pop-Location

if ($exitCode -eq 0) {
    Write-Host "`n[SUCCESS] Build completed successfully!`n" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Build failed with errors!`n" -ForegroundColor Red
    exit $exitCode
}

exit $exitCode
