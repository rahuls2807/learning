using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    /// <summary>
    /// RBI 2FA Compliance: OTP Service via Twilio
    /// Implements One-Time Password for payment authentication
    /// </summary>
    public interface IOtpService
    {
        Task<(bool success, string message, string otpCode, bool devMode)> SendOtpAsync(string phoneNumber, string userId, int bookingId);
        Task<(bool success, string message)> VerifyOtpAsync(string userId, int bookingId, string otpCode, WorkerBookingContext context);
        string GenerateOtp();
    }

    public class OtpService : IOtpService
    {
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioPhoneNumber;
        private readonly ILogger<OtpService> _logger;
        private readonly bool _isTwilioConfigured;
        private readonly bool _showDevOtpOnScreen;

        public OtpService(IConfiguration configuration, ILogger<OtpService> logger)
        {
            _logger = logger;
            
            _twilioAccountSid = configuration["Twilio:AccountSid"] ?? "";
            _twilioAuthToken = configuration["Twilio:AuthToken"] ?? "";
            _twilioPhoneNumber = configuration["Twilio:PhoneNumber"] ?? "";
            _showDevOtpOnScreen = configuration.GetValue("Otp:ShowDevOtpOnScreen", true);
            
            _isTwilioConfigured = !string.IsNullOrWhiteSpace(_twilioAccountSid) 
                && !_twilioAccountSid.Contains("YOUR_TWILIO", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(_twilioAuthToken)
                && !_twilioAuthToken.Contains("YOUR_TWILIO", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(_twilioPhoneNumber)
                && !_twilioPhoneNumber.Contains("YOUR_TWILIO", StringComparison.OrdinalIgnoreCase);

            if (_isTwilioConfigured)
            {
                TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
                _logger.LogInformation("Twilio OTP service initialized");
            }
            else
            {
                _logger.LogWarning("Twilio not configured. OTP will be shown on-screen when enabled (Otp:ShowDevOtpOnScreen).");
            }
        }

        public async Task<(bool success, string message, string otpCode, bool devMode)> SendOtpAsync(string phoneNumber, string userId, int bookingId)
        {
            try
            {
                var normalizedPhone = UpiPaymentHelper.NormalizeIndiaPhone(phoneNumber);
                var otp = GenerateOtp();
                var message = $"Your Indian Worker Mandi payment OTP is: {otp}. Valid for 10 minutes. Do not share this code.";

                if (_isTwilioConfigured)
                {
                    var result = await MessageResource.CreateAsync(
                        body: message,
                        from: new PhoneNumber(_twilioPhoneNumber),
                        to: new PhoneNumber(normalizedPhone)
                    );

                    if (string.IsNullOrWhiteSpace(result.Sid))
                    {
                        _logger.LogError("Twilio returned no message SID for booking {BookingId}", bookingId);
                        return (false, "SMS could not be sent. Check your phone number or try again.", "", false);
                    }

                    _logger.LogInformation("OTP sent via Twilio to {Phone} for booking {BookingId}", normalizedPhone, bookingId);
                    return (true, "OTP sent to your phone by SMS.", otp, false);
                }

                _logger.LogWarning("[DEV OTP] Booking {BookingId} phone {Phone}: {Otp}", bookingId, normalizedPhone, otp);

                if (_showDevOtpOnScreen)
                {
                    return (true,
                        "SMS is not configured. Use the OTP shown below to continue.",
                        otp,
                        true);
                }

                return (false,
                    "SMS OTP is not configured on this server. Ask the administrator to set Twilio credentials in appsettings.",
                    "",
                    false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP for booking {BookingId}", bookingId);
                return (false, $"Failed to send OTP: {ex.Message}", "", false);
            }
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
                    _logger.LogInformation("OTP already verified for user {UserId}, booking {BookingId}", userId, bookingId);
                    return (true, "OTP verified successfully");
                }

                otp.IsVerified = true;
                otp.VerifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation("OTP verified for user {UserId}, booking {BookingId}", userId, bookingId);
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
