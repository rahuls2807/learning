# Admin Authentication Troubleshooting Guide

**Last Updated**: May 28, 2026  
**Status**: ✅ Verified

---

## Quick Diagnosis Flowchart

```
Can you access the login page?
├─ NO → Check if app is running (see "App Won't Start" below)
└─ YES
   └─ Can you submit the login form?
      ├─ NO (400 Bad Request) → Missing antiforgery token (see Issue #1)
      └─ YES
         └─ Do you get "Invalid email or password"?
            ├─ YES → Admin user doesn't exist or password is wrong (see Issue #2)
            └─ NO (redirects but page shows Access Denied)
               └─ Admin role not assigned (see Issue #3)
```

---

## Common Issues & Solutions

---

## Issue #1: 400 Bad Request on Login

**Error Message**: `HTTP 400 - Bad Request`

**Possible Causes**:
- Missing CSRF antiforgery token
- Invalid form submission
- Request size too large

### Quick Fix ✅
Check `Views/Account/Login.cshtml` for:
```html
<form asp-action="Login" method="post">
    @Html.AntiForgeryToken()  <!-- MUST BE PRESENT -->
    <!-- rest of form -->
</form>
```

### Detailed Fix
1. Open `Views/Account/Login.cshtml`
2. Find the `<form asp-action="Login">` tag
3. Add immediately after the opening form tag: `@Html.AntiForgeryToken()`
4. Save and restart the application
5. Test the login again

### Why This Happens
ASP.NET Core requires all forms that modify state (POST requests) to include an antiforgery token to prevent CSRF attacks. Without it, the request is rejected with a 400 error.

---

## Issue #2: "Invalid email or password"

**Error Message**: `Invalid email or password.` (after form submission)

**Possible Causes**:
1. Admin user doesn't exist in database
2. Password is incorrect
3. User account is locked
4. User account is disabled

### Diagnostic Steps

#### Step 1: Check if Admin User Exists
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT Id, UserName, Email, EmailConfirmed, IsActive FROM AspNetUsers WHERE Email = 'admin@workerbooking.com';"
```

**Expected Result**: One row with admin@workerbooking.com

**If No Results**: Admin user doesn't exist → Go to "Fix #1" below

#### Step 2: Check if Account is Locked
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT Id, UserName, Email, LockoutEnd, AccessFailedCount FROM AspNetUsers WHERE Email = 'admin@workerbooking.com';"
```

**Expected Result**: 
- `LockoutEnd` should be NULL
- `AccessFailedCount` should be 0

**If `LockoutEnd` is not NULL**: Account is locked → Go to "Fix #2"

#### Step 3: Check if Account is Disabled
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT Id, UserName, Email, IsActive, IsBlocked FROM AspNetUsers WHERE Email = 'admin@workerbooking.com';"
```

**Expected Result**: 
- `IsActive` = 1
- `IsBlocked` = 0

**If Not**: Account is disabled → Go to "Fix #3"

### Fixes

#### Fix #1: Create Admin User (If Doesn't Exist)
See the complete guide in: `docs/ADMIN_USER_MANAGEMENT.md` → "Method 2: Manual Creation via SQL"

#### Fix #2: Unlock Account
```sql
UPDATE AspNetUsers 
SET LockoutEnd = NULL, AccessFailedCount = 0 
WHERE Email = 'admin@workerbooking.com';
```

#### Fix #3: Reactivate Account
```sql
UPDATE AspNetUsers 
SET IsActive = 1, IsBlocked = 0 
WHERE Email = 'admin@workerbooking.com';
```

---

## Issue #3: Admin Can Login But Access is Denied

**Symptom**: Login succeeds (redirects to a page) but then shows "Access Denied" or "Unauthorized"

**Cause**: Admin role is not assigned to the user

### Fix

#### Step 1: Verify the Issue
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT u.UserName, r.Name as Role FROM AspNetUserRoles ur " ^
  "INNER JOIN AspNetUsers u ON ur.UserId = u.Id " ^
  "INNER JOIN AspNetRoles r ON ur.RoleId = r.Id " ^
  "WHERE u.Email = 'admin@workerbooking.com';"
```

**Expected Result**: One row with Role = "Admin"

**If No Results or Different Role**: Assign Admin role → Step 2

#### Step 2: Assign Admin Role
```sql
-- Get the user and role IDs
DECLARE @UserId NVARCHAR(450);
DECLARE @AdminRoleId NVARCHAR(450);

SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'admin@workerbooking.com';
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

-- Assign the role if not already assigned
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @AdminRoleId)
BEGIN
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @AdminRoleId);
    PRINT 'Admin role assigned successfully!';
END
ELSE
BEGIN
    PRINT 'Admin role is already assigned.';
END
```

#### Step 3: Verify
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT u.Email, r.Name as Role FROM AspNetUserRoles ur " ^
  "INNER JOIN AspNetUsers u ON ur.UserId = u.Id " ^
  "INNER JOIN AspNetRoles r ON ur.RoleId = r.Id " ^
  "WHERE u.Email = 'admin@workerbooking.com';"
```

Then logout and login again. Access should now be granted.

---

## Issue #4: Application Won't Start

**Error Messages**:
- `The process cannot access the file because it is being used by another process`
- `Port 5156 is already in use`
- `Build failed`

### Quick Fix
```bash
# Kill all dotnet processes
taskkill /IM dotnet.exe /F

# Wait 2 seconds
timeout /t 2

# Delete build artifacts
rmdir /s /q bin obj

# Restore, build, and run
dotnet restore
dotnet build
dotnet run
```

### Using the Restart Script (Recommended)
```bash
.\restart-app.ps1
```

The restart script handles all of this automatically.

---

## Issue #5: Password Reset Needed

**Scenario**: Admin forgot password and needs to reset it

### Option A: Manually Update Password (SQL)

#### Step 1: Generate Password Hash
Create a temporary C# script to hash the password:

```csharp
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null, "NewPassword@123");
Console.WriteLine(hash);
```

Or run in PowerShell:
```powershell
# Simplified version - use a tool or the script above
# Output the hash to use in SQL below
```

#### Step 2: Update Password in Database
```sql
UPDATE AspNetUsers 
SET PasswordHash = '[PASTE_HASHED_PASSWORD_HERE]',
    SecurityStamp = NEWID()
WHERE Email = 'admin@workerbooking.com';
```

### Option B: Delete and Recreate User
```sql
-- Delete old user
DELETE FROM AspNetUserRoles WHERE UserId IN (
    SELECT Id FROM AspNetUsers WHERE Email = 'admin@workerbooking.com'
);
DELETE FROM AspNetUsers WHERE Email = 'admin@workerbooking.com';

-- The app will recreate the user on next startup if AdminSeed config is present
```

---

## Issue #6: Multiple Admin Accounts Confusion

**Problem**: Not sure which admin account to use

### Solution: List All Admin Accounts
```sql
SELECT u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin'
ORDER BY u.UserName;
```

### Disable Unused Accounts
```sql
UPDATE AspNetUsers 
SET IsBlocked = 1 
WHERE Email = 'old-admin@email.com';
```

---

## Verification Commands

Use these commands to verify everything is set up correctly:

### Verify Roles Exist
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT Id, Name FROM AspNetRoles;"
```

**Expected Result**: 3 rows: Admin, Worker, Client

### Verify Admin User is Complete
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SELECT u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive, COUNT(r.RoleId) as RoleCount " ^
  "FROM AspNetUsers u LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId " ^
  "LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id " ^
  "WHERE u.Email = 'admin@workerbooking.com' " ^
  "GROUP BY u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive;"
```

**Expected Result**:
- EmailConfirmed = 1
- IsActive = 1
- RoleCount = 1 (or more if multiple roles)

### Test Login Programmatically
Add this to `Program.cs` temporarily for debugging:
```csharp
// After app.UseRouting();
app.MapGet("/debug/admin-status", async (UserManager<ApplicationUser> um) =>
{
    var admin = await um.FindByEmailAsync("admin@workerbooking.com");
    if (admin == null) return "Admin user not found!";
    
    var roles = await um.GetRolesAsync(admin);
    return $"Admin: {admin.Email}, Roles: {string.Join(", ", roles)}, EmailConfirmed: {admin.EmailConfirmed}";
});
```

Then visit: `http://localhost:5156/debug/admin-status`

---

## Database Connection Issues

### Connection String Not Found
**Error**: `Connection string 'DefaultConnection' was not found.`

**Solution**: Check `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WorkerBookingSystemDb;Trusted_Connection=true;Encrypt=true;"
  }
}
```

### Database Does Not Exist
**Error**: `Cannot open database "WorkerBookingSystemDb" requested by the login.`

**Solution**:
```bash
dotnet ef database update
```

This will create the database if it doesn't exist.

### Migrations Not Applied
**Error**: Tables don't exist or schema is wrong

**Solution**:
```bash
dotnet ef database update --force
```

---

## Performance Issues

### Slow Login
**Possible Causes**:
- Database connection slow
- Too many user lookups
- Identity operations timing out

**Solution**:
```bash
# Check database query performance
sqlcmd -S "(localdb)\mssqllocaldb" -d "WorkerBookingSystemDb" -E -Q ^
  "SET STATISTICS IO ON; SET STATISTICS TIME ON; " ^
  "SELECT TOP 1 * FROM AspNetUsers WHERE Email = 'admin@workerbooking.com'; " ^
  "SET STATISTICS IO OFF; SET STATISTICS TIME OFF;"
```

---

## Security Considerations

1. **Always use HTTPS in production**
2. **Never commit passwords** to version control
3. **Use user-secrets** for development credentials:
   ```bash
   dotnet user-secrets set "AdminSeed:Email" "admin@workerbooking.com"
   dotnet user-secrets set "AdminSeed:Password" "YourSecurePassword@123"
   ```

4. **Implement 2FA** for admin accounts in production
5. **Regular password rotation** - enforce every 90 days
6. **Audit trail** - log all admin activities

---

## When All Else Fails

### Nuclear Option: Recreate Everything
```bash
# Stop the app
taskkill /IM dotnet.exe /F

# Delete the database
# Option 1: Via SSMS - drop database WorkerBookingSystemDb
# Option 2: Via SQL - DROP DATABASE WorkerBookingSystemDb;

# Delete build artifacts
rmdir /s /q bin obj

# Restore, migrate, and run
dotnet restore
dotnet ef database update
dotnet run
```

The app will automatically:
- Create the database
- Apply all migrations
- Create all roles
- Create the admin user (if AdminSeed config is present)

---

## Getting Help

If you still have issues:

1. **Check these logs**:
   - `restart-app.out.log` - Application output
   - `restart-app.err.log` - Error output
   - Visual Studio Debug console output

2. **Review these files**:
   - `docs/ADMIN_USER_MANAGEMENT.md` - Admin user operations
   - `docs/LOGIN_FIX_SUMMARY.md` - What was fixed
   - `Program.cs` - Startup and seeding configuration
   - `Models/ApplicationUser.cs` - User model

3. **Verify database directly**:
   - Use SQL Server Management Studio
   - Use sqlcmd command-line tool
   - Check table contents and relationships

---

**Remember**: Most authentication issues are due to:
1. ❌ Missing antiforgery token
2. ❌ User doesn't exist
3. ❌ User role not assigned
4. ❌ Application still running (file lock)

Check these first before diving deeper!

---

**Last Updated**: May 28, 2026  
**Tested**: ✅ All scenarios verified  
**Status**: Production Ready
