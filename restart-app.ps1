param(
    [string]$Url = "http://localhost:5156"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ProjectRoot "WorkerBookingSystem.csproj"
$OutLog = Join-Path $ProjectRoot "restart-app.out.log"
$ErrLog = Join-Path $ProjectRoot "restart-app.err.log"

Write-Host "Restarting WorkerBookingSystem..." -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "URL:     $Url"

Write-Host "Stopping existing WorkerBookingSystem processes..." -ForegroundColor Yellow

try {
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq "dotnet.exe" -and
            $_.CommandLine -and
            ($_.CommandLine.ToLower().Contains("workerbookingsystem") -or $_.CommandLine.Contains($Url))
        } |
        ForEach-Object {
            Write-Host "Killing process $($_.ProcessId)..." -ForegroundColor DarkYellow
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }
}
catch {
    Write-Host "Could not identify project-specific dotnet processes. Continuing safely." -ForegroundColor DarkYellow
}

Start-Sleep -Seconds 2

# Clean build artifacts to avoid file locks
Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 1

Push-Location $ProjectRoot
try {
    Write-Host "Building project..." -ForegroundColor Yellow
    dotnet build $ProjectFile -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }

    if (Test-Path $OutLog) { Remove-Item $OutLog -Force }
    if (Test-Path $ErrLog) { Remove-Item $ErrLog -Force }

    $env:ASPNETCORE_ENVIRONMENT = "Development"

    Write-Host "Starting server..." -ForegroundColor Green
    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $ProjectFile, "--no-build", "--urls", $Url, "--launch-profile", "http") `
        -WorkingDirectory $ProjectRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $OutLog `
        -RedirectStandardError $ErrLog `
        -PassThru

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) {
                Write-Host "Server is running. Open $Url" -ForegroundColor Green
                Write-Host "Process ID: $($process.Id)"
                exit 0
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    Write-Host "Server did not respond in time. Check these logs:" -ForegroundColor Red
    Write-Host "  $OutLog"
    Write-Host "  $ErrLog"
    exit 1
}
finally {
    Pop-Location
}
