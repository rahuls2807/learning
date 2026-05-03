param(
    [string]$Url = "http://localhost:5156"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutLog = Join-Path $ProjectRoot "restart-app.out.log"
$ErrLog = Join-Path $ProjectRoot "restart-app.err.log"

Write-Host "Restarting WorkerBookingSystem..." -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "URL:     $Url"

function Stop-WorkerBookingSystem {
    Write-Host "Stopping existing WorkerBookingSystem processes..." -ForegroundColor Yellow

    Get-Process -Name "WorkerBookingSystem" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

    try {
        Get-CimInstance Win32_Process |
            Where-Object {
                $_.Name -eq "dotnet.exe" -and
                $_.CommandLine -and
                ($_.CommandLine -like "*WorkerBookingSystem*" -or $_.CommandLine -like "*localhost:5156*")
            } |
            ForEach-Object {
                Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
            }
    }
    catch {
        Write-Host "Could not inspect dotnet command lines. Continuing after stopping app executable." -ForegroundColor DarkYellow
    }

    Start-Sleep -Seconds 2
}

function Test-AppReady {
    param([string]$TargetUrl)

    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $TargetUrl -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                return $true
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    return $false
}

Stop-WorkerBookingSystem

Write-Host "Building project..." -ForegroundColor Yellow
Push-Location $ProjectRoot
try {
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }

    Write-Host "Starting server..." -ForegroundColor Green
    if (Test-Path $OutLog) { Remove-Item $OutLog -Force }
    if (Test-Path $ErrLog) { Remove-Item $ErrLog -Force }

    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--no-build", "--urls", $Url) `
        -WorkingDirectory $ProjectRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $OutLog `
        -RedirectStandardError $ErrLog `
        -PassThru

    if (Test-AppReady -TargetUrl $Url) {
        Write-Host "Server is running. Open $Url" -ForegroundColor Green
        Write-Host "Process ID: $($process.Id)"
        exit 0
    }

    Write-Host "Server did not respond in time. Check these logs:" -ForegroundColor Red
    Write-Host "  $OutLog"
    Write-Host "  $ErrLog"
    exit 1
}
finally {
    Pop-Location
}
