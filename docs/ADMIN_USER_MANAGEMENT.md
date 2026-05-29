# Admin User Management Guide

## Overview
This guide explains how admin users are created in the WorkerBookingSystem and how to manually add or recover an admin user if needed.

---

## How Admin Users Are Created

### Method 1: Automatic Seeding (Recommended for Development)

Admin users are automatically created when the application starts if the following conditions are met:

#### Configuration Required:
Add to `appsettings.Development.json`:
```json
{
  "AdminSeed": {
    "Email": "admin@workerbooking.com",
    "Password": "Admin@123456"
  }
}
```

#### In Program.cs Startup:
The application creates the admin user automatically:
```csharp
var adminEmail = builder.Configuration["AdminSeed:Email"];
var adminPassword = builder.Configuration["AdminSeed:Password"];

if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
{
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
```

### Method 2: Manual Creation via SQL (Emergency Recovery)

If automatic seeding fails, create an admin user manually using SQL:

#### Step 1: Generate Password Hash
Create a test application or use the following script to generate a bcrypt password hash:

```csharp
using Microsoft.AspNetCore.Identity;

var passwordHasher = new PasswordHasher<ApplicationUser>();
var hashedPassword = passwordHasher.HashPassword(null, "YourPassword@123");
Console.WriteLine(hashedPassword);
```

#### Step 2: Insert into Database
```sql
-- Insert the admin user
INSERT INTO [AspNetUsers] 
(Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, 
 SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
 LockoutEnd, LockoutEnabled, AccessFailedCount, IsVerified, KycStatus, CreatedAt, IsActive, IsBlocked)
VALUES
('admin-user-id-123', 'admin@workerbooking.com', 'ADMIN@WORKERBOOKING.COM', 
 'admin@workerbooking.com', 'ADMIN@WORKERBOOKING.COM', 1, 
 '[PASTE_HASHED_PASSWORD_HERE]',
 NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 1, 'VERIFIED', GETUTCDATE(), 1, 0);

-- Get the Admin role ID
DECLARE @AdminRoleId NVARCHAR(450);
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

-- Assign Admin role to the user
INSERT INTO [AspNetUserRoles] (UserId, RoleId)
VALUES ('admin-user-id-123', @AdminRoleId);
```

---

## Verify Admin User is Properly Created

Run these SQL queries to verify:

### Check if Admin User Exists:
```sql
SELECT Id, UserName, Email, EmailConfirmed FROM AspNetUsers WHERE Email = 'admin@workerbooking.com';
```

### Check if Admin Role is Assigned:
```sql
SELECT u.UserName, u.Email, r.Name as RoleName 
FROM AspNetUserRoles ur
INNER JOIN AspNetUsers u ON ur.UserId = u.Id
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin@workerbooking.com';
```

Expected output:
```
UserName: admin@workerbooking.com
Email: admin@workerbooking.com
RoleName: Admin
```

---

## Common Issues and Troubleshooting

### Issue 1: 400 Bad Request on Login
**Cause**: Missing CSRF antiforgery token in the Login form

**Fix**: Ensure `[Views/Account/Login.cshtml](../Views/Account/Login.cshtml)` contains:
```html
<form asp-action="Login" method="post">
    @Html.AntiForgeryToken()
    <!-- rest of form -->
</form>
```

### Issue 2: "Invalid email or password" Error
**Possible Causes**:
- Admin user doesn't exist in database
- Password hash is incorrect
- User is locked out

**Fix**:
1. Check if user exists: `SELECT * FROM AspNetUsers WHERE Email = 'admin@workerbooking.com'`
2. Verify PasswordHash is not NULL and is properly formatted
3. Check if user is locked: `LockoutEnd` should be NULL
4. Check if user is disabled: `IsActive` should be 1

### Issue 3: Admin User Can Login But Can't Access Admin Dashboard
**Cause**: Admin role is not assigned to the user

**Fix**: Run the SQL to assign the role (see Method 2, Step 2)

---

## Database Queries Cheat Sheet

### List All Users with Their Roles
```sql
SELECT u.Id, u.UserName, u.Email, STRING_AGG(r.Name, ', ') as Roles
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
GROUP BY u.Id, u.UserName, u.Email;
```

### Find All Admin Users
```sql
SELECT DISTINCT u.Id, u.UserName, u.Email
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin';
```

### Create New Admin (All-in-One Script)
```sql
-- First, generate your hashed password using the script in Method 2
-- Then replace [HASHED_PASSWORD] and run this:

DECLARE @UserId NVARCHAR(450) = NEWID();
DECLARE @AdminRoleId NVARCHAR(450);

-- Get Admin role ID
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

-- Insert user
INSERT INTO [AspNetUsers] 
(Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, 
 SecurityStamp, ConcurrencyStamp, LockoutEnabled, AccessFailedCount, IsVerified, 
 KycStatus, CreatedAt, IsActive, IsBlocked)
VALUES
(@UserId, 'neadmin@workerbooking.com', 'NEADMIN@WORKERBOOKING.COM', 
 'neadmin@workerbooking.com', 'NEADMIN@WORKERBOOKING.COM', 1, 
 '[HASHED_PASSWORD]', NEWID(), NEWID(), 1, 0, 1, 'VERIFIED', GETUTCDATE(), 1, 0);

-- Assign Admin role
INSERT INTO [AspNetUserRoles] (UserId, RoleId)
VALUES (@UserId, @AdminRoleId);

PRINT 'Admin user created successfully!';
```

---

## Password Policy

Passwords for admin users should meet these criteria:
- Minimum 8 characters
- At least one uppercase letter
- At least one number
- At least one special character

Example: `Admin@123456` ✅

---

## Best Practices

1. **Always Enable Email Confirmation**: Set `EmailConfirmed = 1` for admin users
2. **Use Strong Passwords**: Never use weak passwords for admin accounts
3. **Regular Backups**: Before making manual admin changes, backup your database
4. **Audit Trail**: Keep track of who has admin access and when accounts were created
5. **Two-Factor Authentication**: Consider implementing 2FA for admin accounts in production

---

## When to Use Each Method

| Method | When to Use | Pros | Cons |
|--------|-----------|------|------|
| **Automatic Seeding** | Development, first-time setup | Fast, automatic, reproducible | Requires configuration |
| **Manual SQL** | Emergency recovery, production | Direct control, no app changes | Requires DB access, error-prone |

---

## Related Documentation

- [ApplicationUser Model](../Models/ApplicationUser.cs)
- [Program.cs Startup Configuration](../Program.cs)
- [Account Controller](../Controllers/AccountController.cs)
- [Login View](../Views/Account/Login.cshtml)

---

**Last Updated**: May 28, 2026  
**Status**: Verified and Tested ✅
