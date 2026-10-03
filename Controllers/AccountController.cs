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

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
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
