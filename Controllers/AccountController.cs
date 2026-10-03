using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity.UI.Services;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;

namespace WorkerBookingSystem.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            if (!UiLanguageCatalog.IsSupported(culture))
                return BadRequest();

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
        }

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly WorkerBookingContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            WorkerBookingContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-recovery")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var origin = GetPasswordResetOrigin();
                var resetUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { email = user.Email, token },
                    origin?.Scheme,
                    origin?.Authority);

                if (!string.IsNullOrWhiteSpace(resetUrl) && origin != null)
                {
                    var safeUrl = HtmlEncoder.Default.Encode(resetUrl);
                    var body = $"<p>We received a request to reset your Worker Mandi password.</p>" +
                        $"<p><a href=\"{safeUrl}\">Reset your password</a></p>" +
                        "<p>If you did not request this, you can ignore this email. The link can only be used once.</p>";

                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email!, "Reset your Worker Mandi password", body);
                    }
                    catch (Exception exception)
                    {
                        // Do not log the reset URL or token. The browser always receives the same response
                        // whether the email exists or delivery is unavailable.
                        _logger.LogError(exception, "Password reset email could not be delivered.");
                    }
                }
                else
                {
                    _logger.LogError("Password reset link was not sent because PasswordReset:PublicBaseUrl is missing or invalid.");
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return View("ResetPasswordInvalid");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-recovery")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
                return View("ResetPasswordInvalid");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["AccountMessage"] = "Your password was changed successfully.";
            return RedirectToAction(nameof(ChangePassword));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new AccountProfileViewModel
            {
                FirstName = user.BioDescription.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty,
                LastName = user.BioDescription.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = IndianPhoneNumber.ToNationalDigits(user.PhoneNumber),
                Address = user.Address,
                City = user.City,
                State = user.State,
                PinCode = user.PinCode
            };

            var workerProfile = await _context.Workers.AsNoTracking()
                .FirstOrDefaultAsync(profile => profile.UserId == user.Id);
            if (workerProfile != null)
            {
                model.FirstName = workerProfile.FirstName ?? model.FirstName;
                model.LastName = workerProfile.LastName ?? model.LastName;
                model.PhoneNumber = IndianPhoneNumber.ToNationalDigits(workerProfile.PhoneNumber);
            }
            else
            {
                var clientProfile = await _context.Clients.AsNoTracking()
                    .FirstOrDefaultAsync(profile => profile.UserId == user.Id);
                if (clientProfile != null)
                {
                    model.FirstName = clientProfile.FirstName ?? model.FirstName;
                    model.LastName = clientProfile.LastName ?? model.LastName;
                    model.PhoneNumber = IndianPhoneNumber.ToNationalDigits(clientProfile.PhoneNumber);
                    model.Address = clientProfile.Address ?? model.Address;
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AccountProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var emailChanged = !string.Equals(user.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase);
            if (emailChanged && await _userManager.FindByEmailAsync(model.Email.Trim()) is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "That email address is already associated with an account.");
                return View(model);
            }

            var fullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}".Trim();
            var normalizedPhone = IndianPhoneNumber.ToE164(model.PhoneNumber);
            user.PhoneNumber = normalizedPhone;
            user.Address = model.Address.Trim();
            user.City = model.City.Trim();
            user.State = model.State.Trim();
            user.PinCode = model.PinCode.Trim();

            var worker = await _context.Workers.FirstOrDefaultAsync(profile => profile.UserId == user.Id);
            var client = worker == null
                ? await _context.Clients.FirstOrDefaultAsync(profile => profile.UserId == user.Id)
                : null;
            if (worker != null)
            {
                worker.FirstName = model.FirstName.Trim();
                worker.LastName = model.LastName.Trim();
                worker.PhoneNumber = normalizedPhone;
            }
            if (client != null)
            {
                client.FirstName = model.FirstName.Trim();
                client.LastName = model.LastName.Trim();
                client.PhoneNumber = normalizedPhone;
                client.Address = model.Address.Trim();
            }
            if (!emailChanged)
            {
                if (worker != null) worker.Email = user.Email;
                if (client != null) client.Email = user.Email;
            }
            if (worker == null && client == null)
                user.BioDescription = fullName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }
            await _context.SaveChangesAsync();

            if (emailChanged)
            {
                try
                {
                    var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email.Trim());
                    var confirmationUrl = Url.Action(nameof(ConfirmProfileEmail), "Account", new
                    {
                        userId = user.Id,
                        email = model.Email.Trim(),
                        token
                    }, Request.Scheme);
                    if (string.IsNullOrWhiteSpace(confirmationUrl))
                        throw new InvalidOperationException("Could not create an email confirmation link.");

                    var safeUrl = HtmlEncoder.Default.Encode(confirmationUrl);
                    await _emailSender.SendEmailAsync(model.Email.Trim(), "Confirm your Worker Mandi email",
                        $"<p>Confirm this address for your Worker Mandi account:</p><p><a href=\"{safeUrl}\">Confirm email address</a></p>");
                    TempData["ProfileStatus"] = "Profile saved. Check the new email address to confirm your email change.";
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Profile email change confirmation could not be sent for user {UserId}.", user.Id);
                    TempData["ProfileWarning"] = "Your profile was saved, but we could not send the email confirmation. Try changing the email again later.";
                }
            }
            else
            {
                TempData["ProfileStatus"] = "Your profile was saved.";
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        public IActionResult ConfirmProfileEmail(string userId, string email, string token)
        {
            if (!string.Equals(userId, _userManager.GetUserId(User), StringComparison.Ordinal))
                return Forbid();
            return View(new ConfirmProfileEmailViewModel { UserId = userId, Email = email, Token = token });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmProfileEmail(ConfirmProfileEmailViewModel model)
        {
            if (!ModelState.IsValid || !string.Equals(model.UserId, _userManager.GetUserId(User), StringComparison.Ordinal))
                return Forbid();

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var emailResult = await _userManager.ChangeEmailAsync(user, model.Email, model.Token);
            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("ConfirmProfileEmail", model);
            }

            var usernameResult = await _userManager.SetUserNameAsync(user, model.Email);
            if (!usernameResult.Succeeded)
                _logger.LogWarning("Email changed but username could not be synchronized for user {UserId}.", user.Id);

            var worker = await _context.Workers.FirstOrDefaultAsync(profile => profile.UserId == user.Id);
            if (worker != null) worker.Email = model.Email;
            var client = await _context.Clients.FirstOrDefaultAsync(profile => profile.UserId == user.Id);
            if (client != null) client.Email = model.Email;
            await _context.SaveChangesAsync();

            await _signInManager.RefreshSignInAsync(user);
            TempData["ProfileStatus"] = "Your email address was confirmed and updated.";
            return RedirectToAction(nameof(Profile));
        }

        private Uri? GetPasswordResetOrigin()
        {
            var configuredOrigin = _configuration["PasswordReset:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredOrigin))
            {
                if (Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var origin)
                    && (origin.Scheme == Uri.UriSchemeHttps || (_environment.IsDevelopment() && origin.Scheme == Uri.UriSchemeHttp))
                    && string.IsNullOrEmpty(origin.UserInfo)
                    && string.IsNullOrEmpty(origin.Query)
                    && string.IsNullOrEmpty(origin.Fragment)
                    && origin.AbsolutePath == "/")
                {
                    return origin;
                }

                return null;
            }

            if (!_environment.IsDevelopment()
                || !Uri.TryCreate($"{Request.Scheme}://{Request.Host}{Request.PathBase}/", UriKind.Absolute, out var requestOrigin)
                || (requestOrigin.Scheme != Uri.UriSchemeHttps && requestOrigin.Scheme != Uri.UriSchemeHttp))
            {
                return null;
            }

            return requestOrigin;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                model.Password,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, model.RememberMe);
                // Ensure the authentication cookie contains the latest role claims
                // (some deployments may not populate role claims immediately).
                await _signInManager.RefreshSignInAsync(user);

                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (await _userManager.IsInRoleAsync(user, "Worker"))
                    return RedirectToAction("MyBookings", "Worker");
                if (await _userManager.IsInRoleAsync(user, "Client"))
                    return RedirectToAction("MyBookings", "Client");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
