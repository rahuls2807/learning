# Worker Booking System - Application Guide

## Purpose

Worker Booking System connects clients with skilled workers, tracks bookings, splits payments between online and cash-to-worker payments, and gives admins visibility into operational earnings, worker payouts, and outstanding balances.

## Main Roles

### Client
- Registers and logs in with a private account.
- Searches active workers by name or skill.
- Books a worker.
- Pays online in full or partial amounts.
- Records cash paid directly to the worker.
- Updates the status of their own booking only.
- Cannot see other clients or other clients' bookings.

### Worker
- Registers and logs in with a private account.
- Manages their own profile and availability.
- Sees assigned jobs.
- Sees worker payout at 90% of client charge, preserving 10% business margin.
- Cannot see client lists or admin financial controls.

### Admin
- Logs in through the normal login page.
- Admin links are hidden unless the logged-in user is in the Admin role.
- Manages rates, bookings, reports, and admin accounts.
- Sees full client charge, worker payout, platform profit, online collected, cash paid to workers, and outstanding balances.

## Important Code Areas

### Startup and Security

File: `Program.cs`

- Configures MVC, EF Core, SQL Server, and ASP.NET Identity.
- Applies migrations at startup.
- Creates roles: `Admin`, `Worker`, `Client`.
- Seeds the first admin only when `AdminSeed:Email` and `AdminSeed:Password` are provided through User Secrets, environment variables, or deployment secret settings.

Local admin secrets:

```powershell
dotnet user-secrets set "AdminSeed:Email" "admin@example.com"
dotnet user-secrets set "AdminSeed:Password" "StrongPassword@123"
```

### Data Model

File: `Data/WorkerBookingContext.cs`

The context inherits from `IdentityDbContext<ApplicationUser>`, so application users and business tables live in the same EF Core context.

Important tables:
- `Workers`
- `Clients`
- `Bookings`
- `HourlyRates`
- ASP.NET Identity tables, such as `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles`

Scale indexes are configured for:
- worker search and filtering
- active worker lookup
- worker phone uniqueness
- client booking history
- worker booking history
- booking status/payment status
- active hourly rate lookup

### Worker Search at Scale

Files:
- `Controllers/ClientController.cs`
- `Controllers/WorkerController.cs`
- `Models/ViewModels/PagedResult.cs`

Worker browsing uses:
- `AsNoTracking()`
- filtered queries
- `Skip()` / `Take()` pagination
- projection into `WorkerSearchItemViewModel`

This avoids loading millions of worker records into application memory.

### Booking and Payment Flow

Files:
- `Models/Booking.cs`
- `Controllers/ClientController.cs`
- `Controllers/PaymentController.cs`
- `Views/Client/MyBookings.cshtml`
- `Views/Payment/Pay.cshtml`

Payments are tracked as:
- `AmountPaidOnline`
- `AmountPaidToWorker`
- remaining balance
- `PaymentStatus`

The payment form does not store card details. It records the payment result and reference only.

### Admin Operations

Files:
- `Controllers/AdminController.cs`
- `Views/Admin/Dashboard.cshtml`
- `Views/Admin/ManageBookings.cshtml`

Admin dashboard includes:
- worker count
- client count
- total bookings
- completed earnings
- active jobs
- online collected
- paid to workers
- outstanding balance
- top demand skills

## Handling Millions of Workers

The application is now structured to avoid the main anti-pattern: loading all workers at once.

Current scale foundations:
- paginated worker search
- database indexes
- projection view models
- no-tracking read queries
- role-based access control
- server-side ownership checks

For production-scale millions, use:
- Azure SQL or full SQL Server, not LocalDB
- App Service, Container Apps, or AKS
- Application Insights
- Redis for hot search/filter caching
- background jobs for reports and analytics
- CDN/static asset caching
- keyset pagination for very deep paging
- full-text search or Azure AI Search for advanced worker discovery

## Smart Feature Roadmap

High-value next features:
- worker ratings and verified badges
- location/city-based matching
- availability-aware search
- booking conflict detection
- client favorites
- worker response/acceptance workflow
- cancellation rules and fees
- automated payout ledger
- admin audit log
- notification system for booking/payment/status changes
- document verification for workers
- search relevance ranking based on completion rate and response speed

## Security Notes

- Do not commit passwords in `appsettings.json`.
- Use User Secrets locally.
- Use environment variables or Azure Key Vault in production.
- Keep admin creation inside the admin portal after first seed admin.
- Keep payment card handling delegated to a real payment provider before production.
- Always verify user ownership on the server, not only in views.

## In-App AI Assistant

Files:
- `Controllers/ChatbotController.cs`
- `Models/ViewModels/ChatbotViewModels.cs`
- `Views/Shared/_Layout.cshtml`
- `wwwroot/js/site.js`
- `wwwroot/css/site.css`

The floating assistant helps users understand how to use the application. It can answer questions about:
- booking workers
- paying online
- recording cash paid directly to workers
- client booking status updates
- worker jobs and payout
- admin dashboards and reports

The browser never receives the OpenAI API key. The JavaScript widget posts the question to `/Chatbot/Ask`, and the server calls OpenAI only when an API key is configured.

Local setup:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
```

Production setup:

```powershell
setx OPENAI_API_KEY "your-openai-api-key"
```

If no API key is configured, the assistant still works with built-in application help responses.

## Local Run

```powershell
dotnet build
.\restart-app.ps1
```

Open:

```text
http://localhost:5156/
```
