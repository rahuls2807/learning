$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Manager = Join-Path $ProjectRoot "manage-app.ps1"
$TaskName = "WorkerBookingSystem-LocalApp-Recovery"
$PowerShell = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\powershell.exe"
$User = "$env:USERDOMAIN\$env:USERNAME"

$arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$Manager`" -Action Start -SkipBuild"
$action = New-ScheduledTaskAction -Execute $PowerShell -Argument $arguments -WorkingDirectory $ProjectRoot
$atLogOn = New-ScheduledTaskTrigger -AtLogOn -User $User
$periodic = New-ScheduledTaskTrigger -Daily -At "12:00AM" `
    -RepetitionInterval (New-TimeSpan -Minutes 2) `
    -RepetitionDuration (New-TimeSpan -Days 1)
$principal = New-ScheduledTaskPrincipal -UserId $User -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 2) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask -TaskName $TaskName `
    -Description "Starts Worker Booking System at interactive logon and checks/restarts it every two minutes." `
    -Action $action `
    -Trigger @($atLogOn, $periodic) `
    -Principal $principal `
    -Settings $settings `
    -Force | Out-Null

Write-Host "Installed scheduled task '$TaskName' for $User." -ForegroundColor Green
Write-Host "It runs in your interactive account so LocalDB remains accessible."
Write-Host "Remove it with uninstall-app-watchdog.ps1."
