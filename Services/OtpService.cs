using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Services.Sms;

namespace WorkerBookingSystem.Services
{
    public interface IOtpService
    {
        Task<(bool success, string message, string otpCode, bool devMode)> SendOtpAsync(string phoneNumber, string userId, int bookingId);
        Task<(bool success, string message)> VerifyOtpAsync(string userId, int bookingId, string otpCode, WorkerBookingContext context);
        string GenerateOtp();
    }

    public class OtpService : IOtpService
    {
        private readonly IEnumerable<ISmsOtpSender> _senders;
        private readonly ILogger<OtpService> _logger;
        private readonly bool _showDevOtpOnScreen;
        private readonly string _preferredProvider;

        public OtpService(
            IEnumerable<ISmsOtpSender> senders,
            IConfiguration configuration,
            ILogger<OtpService> logger)
        {
            _senders = senders.OrderBy(s => s.Priority);
            _logger = logger;
            _showDevOtpOnScreen = configuration.GetValue("Otp:ShowDevOtpOnScreen", true);
            _preferredProvider = configuration["Sms:Provider"] ?? "Auto";
        }

        public async Task<(bool success, string message, string otpCode, bool devMode)> SendOtpAsync(string phoneNumber, string userId, int bookingId)
        {
            try
            {
                var normalizedPhone = UpiPaymentHelper.NormalizeIndiaPhone(phoneNumber);
                if (normalizedPhone.Replace("+", "").Length < 12)
                {
                    return (false, "Enter a valid 10-digit mobile number with country code (+91).", "", false);
                }

                var otp = GenerateOtp();
                var sender = ResolveSender();

                if (sender != null)
                {
                    var (sent, error) = await sender.SendOtpAsync(normalizedPhone, otp, bookingId);
                    if (sent)
                    {
                        return (true, $"OTP sent by SMS to {MaskPhone(normalizedPhone)}.", otp, false);
                    }

                    _logger.LogWarning("Primary SMS provider {Provider} failed for booking {BookingId}: {Error}", sender.ProviderName, bookingId, error);

                    var fallback = _senders.FirstOrDefault(s => s.IsConfigured && s.ProviderName != sender.ProviderName);
                    if (fallback != null)
                    {
                        var (fallbackSent, fallbackError) = await fallback.SendOtpAsync(normalizedPhone, otp, bookingId);
                        if (fallbackSent)
                        {
                            return (true, $"OTP sent by SMS to {MaskPhone(normalizedPhone)}.", otp, false);
                        }

                        _logger.LogWarning("Fallback SMS provider {Provider} failed: {Error}", fallback.ProviderName, fallbackError);
                    }
                }

                _logger.LogWarning("[DEV OTP] Booking {BookingId} phone {Phone}", bookingId, normalizedPhone);

                if (_showDevOtpOnScreen)
                {
                    return (true,
                        "SMS provider is not configured. Use the OTP shown on this screen.",
                        otp,
                        true);
                }

                return (false,
                    "SMS OTP is not configured. Set MSG91 or Twilio credentials in user secrets.",
                    "",
                    false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP for booking {BookingId}", bookingId);
                return (false, "Failed to send OTP. Please try again.", "", false);
            }
        }

        private ISmsOtpSender? ResolveSender()
        {
            if (_preferredProvider.Equals("Msg91", StringComparison.OrdinalIgnoreCase))
            {
                return _senders.FirstOrDefault(s => s.ProviderName == "MSG91" && s.IsConfigured)
                    ?? _senders.FirstOrDefault(s => s.IsConfigured);
            }

            if (_preferredProvider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                return _senders.FirstOrDefault(s => s.ProviderName == "Twilio" && s.IsConfigured)
                    ?? _senders.FirstOrDefault(s => s.IsConfigured);
            }

            return _senders.FirstOrDefault(s => s.IsConfigured);
        }

        private static string MaskPhone(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length < 4)
            {
                return phone;
            }

            return "******" + digits[^4..];
        }

        public async Task<(bool success, string message)> VerifyOtpAsync(string userId, int bookingId, string otpCode, WorkerBookingContext context)
        {
            try
            {
                var otp = await context.OtpVerifications
                    .Where(o => o.UserId == userId && o.BookingId == bookingId)
                    .OrderByDescending(o => o.GeneratedAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    return (false, "No valid OTP found. Please request a new one.");
                }

                if ((DateTime.UtcNow - otp.GeneratedAt).TotalMinutes > 10)
                {
                    if (!otp.IsVerified)
                    {
                        otp.AttemptsRemaining = 0;
                        await context.SaveChangesAsync();
                        return (false, "OTP has expired. Please request a new one.");
                    }
                }

                if (!otp.IsVerified && otp.AttemptsRemaining <= 0)
                {
                    return (false, "Maximum OTP attempts exceeded. Please request a new one.");
                }

                if (otp.OtpCode != otpCode)
                {
                    if (!otp.IsVerified)
                    {
                        otp.AttemptsRemaining--;
                        await context.SaveChangesAsync();
                        return (false, $"Invalid OTP. Attempts remaining: {otp.AttemptsRemaining}");
                    }

                    return (false, "Invalid OTP. Please request a new one.");
                }

                if (otp.IsVerified)
                {
                    return (true, "OTP verified successfully");
                }

                otp.IsVerified = true;
                otp.VerifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                return (true, "OTP verified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP");
                return (false, "Error verifying OTP. Please try again.");
            }
        }

        public string GenerateOtp()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return value.ToString("D6");
        }
    }
}
