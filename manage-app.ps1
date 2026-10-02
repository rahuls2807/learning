param(
    [ValidateSet("Start", "Stop", "Restart", "Status")]
    [string]$Action = "Status",
    [string]$Url = "http://localhost:5156",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ProjectRoot "WorkerBookingSystem.csproj"
$AssemblyPath = Join-Path $ProjectRoot "bin\Release\net8.0\WorkerBookingSystem.dll"
$AppHostPaths = @(
    (Join-Path $ProjectRoot "bin\Release\net8.0\WorkerBookingSystem.exe"),
    (Join-Path $ProjectRoot "bin\Debug\net8.0\WorkerBookingSystem.exe")
)
$DataDirectory = Join-Path $env:LOCALAPPDATA "WorkerBookingSystem"
$PidFile = Join-Path $DataDirectory "app.pid"
$OutLog = Join-Path $DataDirectory "app.out.log"
$ErrLog = Join-Path $DataDirectory "app.err.log"
$BaseUri = [Uri]$Url
$HealthUrl = "$($Url.TrimEnd('/'))/health/ready"

function Get-WorkerBookingProcess {
    $processes = Get-CimInstance Win32_Process
    foreach ($candidate in $processes) {
        $commandLine = [string]$candidate.CommandLine
        $isAppHost = $candidate.Name -eq "WorkerBookingSystem.exe" -and
            $AppHostPaths -contains $candidate.ExecutablePath
        $isDotnetHost = $candidate.Name -eq "dotnet.exe" -and $commandLine -and (
                $commandLine.IndexOf($AssemblyPath, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                ($commandLine.IndexOf($ProjectFile, [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
                 $commandLine.IndexOf("run", [StringComparison]::OrdinalIgnoreCase) -ge 0))
        if ($isAppHost -or $isDotnetHost) {
            return $candidate
        }
    }

    return $null
}

function Get-HttpStatusCode([string]$RequestUrl) {
    try {
        $response = Invoke-WebRequest -Uri $RequestUrl -UseBasicParsing -TimeoutSec 3
        return [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            return [int]$_.Exception.Response.StatusCode
        }

        return 0
    }
}

function Show-AppStatus {
    $process = Get-WorkerBookingProcess
    $rootStatus = Get-HttpStatusCode $Url
    $readyStatus = Get-HttpStatusCode $HealthUrl

    if ($process -and $rootStatus -eq 200 -and $readyStatus -eq 200) {
        Write-Host "Worker Booking System is RUNNING and database-ready." -ForegroundColor Green
        Write-Host "URL:       $Url"
        Write-Host "Process:   $($process.ProcessId)"
        Write-Host "Readiness: HTTP $readyStatus"
        Write-Host "Logs:      $DataDirectory"
        return 0
    }

    if ($process) {
        Write-Host "App process exists, but readiness is not healthy (home HTTP $rootStatus; database readiness HTTP $readyStatus)." -ForegroundColor Yellow
        Write-Host "Inspect: $ErrLog"
        return 1
    }

    Write-Host "Worker Booking System is NOT running at $Url." -ForegroundColor Yellow
    if ($rootStatus -ne 0) {
        Write-Host "The URL is occupied or returned HTTP $rootStatus; inspect that listener before starting another copy." -ForegroundColor Yellow
    }
    Write-Host "Start it with launch-app.bat or: .\manage-app.ps1 Start"
    return 1
}

function Stop-WorkerBookingApp {
    $process = Get-WorkerBookingProcess
    if (-not $process) {
        Write-Host "No WorkerBookingSystem dotnet process found." -ForegroundColor Yellow
        return 0
    }

    Write-Host "Stopping WorkerBookingSystem process $($process.ProcessId)..." -ForegroundColor Yellow
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        if (-not (Get-WorkerBookingProcess)) {
            Write-Host "Application stopped." -ForegroundColor Green
            return 0
        }
        Start-Sleep -Milliseconds 500
    }

    Write-Error "The application process did not stop."
    return 1
}

function Build-WorkerBookingApp {
    Write-Host "Building Worker Booking System (Release)..." -ForegroundColor Cyan
    Push-Location $ProjectRoot
    try {
        & dotnet build $ProjectFile -c Release --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Release build failed with exit code $LASTEXITCODE. A running app has not been stopped."
            return 1
        }
    }
    finally {
        Pop-Location
    }

    return 0
}

function Start-WorkerBookingApp {
    param([switch]$SkipBuild)

    $existing = Get-WorkerBookingProcess
    if ($existing) {
        if ((Get-HttpStatusCode $HealthUrl) -eq 200) {
            Write-Host "Application is already running and ready at $Url (PID $($existing.ProcessId))." -ForegroundColor Green
            return 0
        }

        Write-Host "Stopping the unhealthy existing app process before restart..." -ForegroundColor Yellow
        if ((Stop-WorkerBookingApp) -ne 0) { return 1 }
    }

    if ((Get-HttpStatusCode $Url) -ne 0) {
        Write-Error "Port $($BaseUri.Port) is already in use by another process. Stop that listener or choose another -Url."
        return 1
    }

    if (-not $SkipBuild) {
        if ((Build-WorkerBookingApp) -ne 0) { return 1 }
    }

    if (-not (Test-Path $AssemblyPath)) {
        Write-Error "Application assembly not found: $AssemblyPath. Build the project first."
        return 1
    }

    New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = $Url

    Write-Host "Starting application at $Url..." -ForegroundColor Cyan
    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @($AssemblyPath) `
        -WorkingDirectory $ProjectRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $OutLog `
        -RedirectStandardError $ErrLog `
        -PassThru
    Set-Content -Path $PidFile -Value $process.Id

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($process.HasExited) {
            Write-Host "Application exited during startup (exit code $($process.ExitCode))." -ForegroundColor Red
            Write-Host "Error log: $ErrLog"
            return 1
        }

        if ((Get-HttpStatusCode $HealthUrl) -eq 200 -and (Get-HttpStatusCode $Url) -eq 200) {
            Write-Host "Application is running and database-ready." -ForegroundColor Green
            Write-Host "Open Chrome or another browser at: $Url"
            Write-Host "Process ID: $($process.Id)"
            Write-Host "Logs:      $DataDirectory"
            return 0
        }

        Start-Sleep -Seconds 1
    }

    Write-Host "Application did not become ready within 60 seconds." -ForegroundColor Red
    Write-Host "Error log: $ErrLog"
    Write-Host "Output log: $OutLog"
    return 1
}

switch ($Action) {
    "Status" { exit (Show-AppStatus) }
    "Stop" { exit (Stop-WorkerBookingApp) }
    "Start" { exit (Start-WorkerBookingApp) }
    "Restart" {
        if ((Build-WorkerBookingApp) -ne 0) { exit 1 }
        $stopResult = Stop-WorkerBookingApp
        if ($stopResult -ne 0) { exit $stopResult }
        exit (Start-WorkerBookingApp -SkipBuild)
    }
}