using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WorkerBookingSystem.Services.Sms
{
    /// <summary>
    /// MSG91 OTP API — common choice for Indian apps (DLT-compliant templates).
    /// Docs: https://docs.msg91.com/reference/send-otp
    /// </summary>
    public class Msg91SmsOtpSender : ISmsOtpSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Msg91SmsOtpSender> _logger;
        private readonly string _authKey;
        private readonly string _templateId;
        private readonly string _senderId;

        public string ProviderName => "MSG91";
        public int Priority => 1;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_authKey)
            && !_authKey.Contains("YOUR_MSG91", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_templateId)
            && !_templateId.Contains("YOUR_MSG91", StringComparison.OrdinalIgnoreCase);

        public Msg91SmsOtpSender(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<Msg91SmsOtpSender> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _authKey = configuration["Msg91:AuthKey"] ?? "";
            _templateId = configuration["Msg91:TemplateId"] ?? "";
            _senderId = configuration["Msg91:SenderId"] ?? "IWMANDI";
        }

        public async Task<(bool success, string? error)> SendOtpAsync(string normalizedPhone, string otp, int bookingId)
        {
            if (!IsConfigured)
            {
                return (false, "MSG91 is not configured.");
            }

            try
            {
                var mobile = ToMsg91Mobile(normalizedPhone);
                var client = _httpClientFactory.CreateClient("Msg91");

                var payload = new
                {
                    template_id = _templateId,
                    mobile,
                    otp,
                    otp_length = otp.Length,
                    sender = _senderId
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://control.msg91.com/api/v5/otp");
                request.Headers.Add("authkey", _authKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("MSG91 OTP failed for booking {BookingId}. Status {Status}. Body: {Body}", bookingId, response.StatusCode, body);
                    return (false, "SMS could not be sent via MSG91.");
                }

                _logger.LogInformation("OTP sent via MSG91 to {Mobile} for booking {BookingId}", mobile, bookingId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MSG91 OTP error for booking {BookingId}", bookingId);
                return (false, ex.Message);
            }
        }

        private static string ToMsg91Mobile(string normalizedPhone)
        {
            var digits = new string(normalizedPhone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
            {
                return "91" + digits;
            }

            if (digits.StartsWith("91") && digits.Length == 12)
            {
                return digits;
            }

            return digits.TrimStart('0');
        }
    }
}
