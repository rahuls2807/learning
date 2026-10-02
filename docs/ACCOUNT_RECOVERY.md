# Registration and password recovery

## Create an account before signing in

From the home page or the signed-out navigation, choose **Create account**. The account chooser links to the existing client or worker registration form. These registrations create the Identity account and corresponding client/worker profile, assign the matching role, and sign the new user in. The login page and each registration form also link between sign-in and account creation.

## Forgot and reset password

1. Choose **Forgot your password?** from the login form.
2. Enter the account email. The response is identical whether the account exists, preventing account enumeration.
3. For a confirmed account, the app creates an ASP.NET Identity one-time reset token and sends an absolute HTTPS reset link by SMTP.
4. The link opens the reset form; Identity enforces password policy, expiry, and one-time token use.
5. Signed-in users can change their password from **Password** in navigation after entering their current password.

The app does not sign a user in after a password reset. They return to the login page and use the new password.

## Configure email delivery

SMTP credentials are never checked into the repository. Configure these keys using User Secrets for development or the deployment secret store/environment for production:

- `Email:Smtp:Host`
- `Email:Smtp:Port` (defaults to 587)
- `Email:Smtp:EnableSsl` (defaults to true; use your provider's STARTTLS settings)
- `Email:Smtp:FromAddress`
- `Email:Smtp:FromName` (optional; defaults to Worker Mandi)
- `Email:Smtp:UserName` (if required by the relay)
- `Email:Smtp:Password` (if required; secret only)
- `PasswordReset:PublicBaseUrl` (the canonical app origin, such as `https://workers.example.com`; required in production)

Environment variable forms use double underscores, for example `Email__Smtp__Host` and `Email__Smtp__Password`. Set the public app base URL/host correctly so ASP.NET Core can generate correct HTTPS reset links behind your configured trusted reverse proxy. Verify that outbound SMTP is allowed by the hosting environment.

Set the canonical origin with `PasswordReset__PublicBaseUrl`. Production reset links require this configured HTTPS origin; the app does not trust an arbitrary incoming `Host` header. Development can fall back to the local request origin.

When delivery is not configured or fails, the UI still returns the same generic confirmation and the server logs a delivery error without logging the password reset token. Configure and test SMTP before relying on password recovery in production.
