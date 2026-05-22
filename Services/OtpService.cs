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
        Task<(bool success, string message)> SendOtpAsync(string phoneNumber, string userId, int bookingId);
        Task<(bool success, string message)> VerifyOtpAsync(string userId, int bookingId, string otpCode, WorkerBookingContext context);
        string GenerateOtp();
    }

    public class OtpService : IOtpService
    {
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioPhoneNumber;
        private readonly ILogger<OtpService> _logger;

        public OtpService(IConfiguration configuration, ILogger<OtpService> logger)
        {
            _twilioAccountSid = configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio AccountSid not configured");
            _twilioAuthToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio AuthToken not configured");
            _twilioPhoneNumber = configuration["Twilio:PhoneNumber"] ?? throw new InvalidOperationException("Twilio PhoneNumber not configured");
            _logger = logger;

            // Initialize Twilio
            TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
        }

        public async Task<(bool success, string message)> SendOtpAsync(string phoneNumber, string userId, int bookingId)
        {
            try
            {
                var otp = GenerateOtp();
                var message = $"Your WorkerBookingSystem payment OTP is: {otp}. Valid for 10 minutes. Do not share this code.";

                // Send SMS via Twilio
                var result = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_twilioPhoneNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                _logger.LogInformation($"OTP sent to {phoneNumber} for booking {bookingId}");

                return (true, "OTP sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending OTP: {ex.Message}");
                return (false, "Failed to send OTP. Please try again.");
            }
        }

        public async Task<(bool success, string message)> VerifyOtpAsync(string userId, int bookingId, string otpCode, WorkerBookingContext context)
        {
            try
            {
                // Get the most recent OTP for this user and booking
                var otp = await context.OtpVerifications
                    .Where(o => o.UserId == userId && o.BookingId == bookingId && !o.IsVerified)
                    .OrderByDescending(o => o.GeneratedAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    return (false, "No valid OTP found. Please request a new one.");
                }

                // Check if OTP has expired (10 minutes validity)
                if ((DateTime.UtcNow - otp.GeneratedAt).TotalMinutes > 10)
                {
                    otp.AttemptsRemaining = 0;
                    await context.SaveChangesAsync();
                    return (false, "OTP has expired. Please request a new one.");
                }

                // Check attempts remaining
                if (otp.AttemptsRemaining <= 0)
                {
                    return (false, "Maximum OTP attempts exceeded. Please request a new one.");
                }

                // Verify OTP code
                if (otp.OtpCode != otpCode)
                {
                    otp.AttemptsRemaining--;
                    await context.SaveChangesAsync();
                    return (false, $"Invalid OTP. Attempts remaining: {otp.AttemptsRemaining}");
                }

                // Mark as verified
                otp.IsVerified = true;
                otp.VerifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation($"OTP verified for user {userId}, booking {bookingId}");
                return (true, "OTP verified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying OTP: {ex.Message}");
                return (false, "Error verifying OTP. Please try again.");
            }
        }

        /// <summary>
        /// Generate cryptographically secure 6-digit OTP
        /// </summary>
        public string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[4];
                rng.GetBytes(tokenData);
                int randomNumber = Math.Abs(BitConverter.ToInt32(tokenData, 0));
                return (randomNumber % 1000000).ToString("D6");
            }
        }
    }
}
