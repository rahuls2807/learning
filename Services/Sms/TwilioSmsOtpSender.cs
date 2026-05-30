using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace WorkerBookingSystem.Services.Sms
{
    public class TwilioSmsOtpSender : ISmsOtpSender
    {
        private readonly ILogger<TwilioSmsOtpSender> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public string ProviderName => "Twilio";
        public int Priority => 2;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_accountSid)
            && !_accountSid.Contains("YOUR_TWILIO", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_authToken)
            && !_authToken.Contains("YOUR_TWILIO", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_fromNumber)
            && !_fromNumber.Contains("YOUR_TWILIO", StringComparison.OrdinalIgnoreCase);

        public TwilioSmsOtpSender(IConfiguration configuration, ILogger<TwilioSmsOtpSender> logger)
        {
            _logger = logger;
            _accountSid = configuration["Twilio:AccountSid"] ?? "";
            _authToken = configuration["Twilio:AuthToken"] ?? "";
            _fromNumber = configuration["Twilio:PhoneNumber"] ?? "";

            if (IsConfigured)
            {
                TwilioClient.Init(_accountSid, _authToken);
                _logger.LogInformation("Twilio SMS OTP sender ready");
            }
        }

        public async Task<(bool success, string? error)> SendOtpAsync(string normalizedPhone, string otp, int bookingId)
        {
            if (!IsConfigured)
            {
                return (false, "Twilio is not configured.");
            }

            try
            {
                var message = $"Your Indian Worker Mandi payment OTP is {otp}. Valid for 10 minutes. Do not share this code.";
                var result = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_fromNumber),
                    to: new PhoneNumber(normalizedPhone));

                if (string.IsNullOrWhiteSpace(result.Sid))
                {
                    return (false, "Twilio did not return a message id.");
                }

                _logger.LogInformation("OTP sent via Twilio to {Phone} for booking {BookingId}", normalizedPhone, bookingId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio OTP error for booking {BookingId}", bookingId);
                return (false, ex.Message);
            }
        }
    }
}
