# Local app startup, status, and recovery (Windows)

The local ASP.NET Core app is **not a Windows Service**. It runs as a `dotnet` process in your signed-in Windows account and uses that account's SQL Server LocalDB. The normal local URL is `http://localhost:5156/`.

## Quick actions

Run these files by double-clicking them in Explorer, or from PowerShell in the repository folder:

| Action | File | PowerShell equivalent |
| --- | --- | --- |
| Start in background (builds Release first) | `start-app.bat` or `launch-app.bat` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\manage-app.ps1 -Action Start` |
| Check health | `status-app.bat` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\manage-app.ps1 -Action Status` |
| Restart (builds before stopping the healthy process) | `restart-app.bat` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\manage-app.ps1 -Action Restart` |
| Stop | `stop-app.bat` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\manage-app.ps1 -Action Stop` |

On success, the start/restart action prints the URL and process ID. Open `http://localhost:5156/` in Chrome, Edge, Firefox, or another browser on this same Windows PC. Keep the PC awake and signed in while using the app. `localhost` refers to the device making the browser request; it is not available from another computer or phone unless the app is deliberately bound/exposed to a private network.

The app responds to `/health/live` when the web process is up; `/health/ready` returns HTTP 200 only when SQL can be reached. The status action requires both `/health/ready` and the home page to return HTTP 200.

## Logs and common problems

Logs are kept outside the repository at `%LOCALAPPDATA%\WorkerBookingSystem\app.out.log` and `app.err.log`. Open them when startup says it did not become ready.

- **Port already in use:** status reports if another program answers on port 5156. Identify that program before stopping it; the manager will not kill unknown listeners. You can run with another URL, for example `-Url http://localhost:5160`, but keep that same URL for status/restart.
- **Database not ready:** confirm `MSSQLLocalDB` is available to the same Windows user running the app; check the error log and database connection configuration.
- **Browser still shows an old page:** hard-refresh Chrome (`Ctrl+F5`) or open the same URL in a private window; assets use versioned URLs but the browser may retain an old tab.
- **No connection:** start the app first, then browse to the explicit `http://localhost:5156/` address. The URL only works while the process is running.
- **Build failure during restart:** restart builds before stopping the currently healthy process, so it leaves that process up if the new build fails.

The restart command is a local convenience, not a production deployment or high-availability service. A Windows reboot/logoff stops the interactive app unless you enable the optional watchdog.

## Optional start/recovery watchdog

Run `install-app-watchdog.ps1` with PowerShell as the current user to register a Task Scheduler task. It starts the app at interactive sign-in and checks it every two minutes, starting it again if the process or database readiness fails. It runs as the interactive user so LocalDB is accessible. Remove it with `uninstall-app-watchdog.ps1`. This is best-effort local development recovery, not a substitute for Windows Service/production hosting, backups, or monitoring.

Do not run a second copy manually while the watchdog manages startup; the scripts detect their own app process and avoid launching duplicates.
