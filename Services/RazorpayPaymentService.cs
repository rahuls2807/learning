using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace WorkerBookingSystem.Services
{
    public interface IRazorpayPaymentService
    {
        bool IsConfigured { get; }
        Task<Dictionary<string, object>> CreateOrderAsync(int bookingId, decimal amount, string clientEmail, string clientPhone);
        Task<bool> VerifyPaymentSignatureAsync(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature);
        Task<Dictionary<string, object>> CapturePaymentAsync(string razorpayPaymentId, decimal amount);
        Task<Dictionary<string, object>> RefundPaymentAsync(string razorpayPaymentId, decimal amount);
    }

    public class RazorpayPaymentService : IRazorpayPaymentService
    {
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly ILogger<RazorpayPaymentService> _logger;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_keyId)
            && !_keyId.Contains("YOUR_RAZORPAY", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_keySecret)
            && !_keySecret.Contains("YOUR_RAZORPAY", StringComparison.OrdinalIgnoreCase);

        public RazorpayPaymentService(IConfiguration configuration, ILogger<RazorpayPaymentService> logger)
        {
            _keyId = configuration["Razorpay:KeyId"] ?? "";
            _keySecret = configuration["Razorpay:KeySecret"] ?? "";
            _logger = logger;

            if (!IsConfigured)
            {
                _logger.LogWarning("Razorpay is not configured. Use appsettings or user secrets for KeyId and KeySecret.");
            }
        }

        public async Task<Dictionary<string, object>> CreateOrderAsync(int bookingId, decimal amount, string clientEmail, string clientPhone)
        {
            if (!IsConfigured)
            {
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", "Razorpay is not configured. Add Razorpay:KeyId and Razorpay:KeySecret in configuration." }
                };
            }

            try
            {
                var client = new RazorpayClient(_keyId, _keySecret);
                var amountInPaise = (long)(amount * 100);

                var options = new Dictionary<string, object>
                {
                    { "amount", amountInPaise },
                    { "currency", "INR" },
                    { "receipt", $"booking_{bookingId}_{DateTime.UtcNow:yyyyMMddHHmmss}" },
                    { "notes", new Dictionary<string, object>
                        {
                            { "booking_id", bookingId },
                            { "email", clientEmail },
                            { "phone", clientPhone }
                        }
                    }
                };

                var order = client.Order.Create(options);
                string orderId = order["id"]?.ToString() ?? string.Empty;
                _logger.LogInformation("Razorpay order created for booking {BookingId}", bookingId);

                return new Dictionary<string, object>
                {
                    { "order_id", orderId },
                    { "amount", amountInPaise },
                    { "key_id", _keyId },
                    { "success", true }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Razorpay order for booking {BookingId}", bookingId);
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }

        public async Task<bool> VerifyPaymentSignatureAsync(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            if (!IsConfigured)
            {
                return false;
            }

            try
            {
                var expectedSignature = GenerateSignature(razorpayOrderId, razorpayPaymentId);
                var isValid = expectedSignature == razorpaySignature;

                if (!isValid)
                {
                    _logger.LogWarning("Signature verification failed for payment {PaymentId}", razorpayPaymentId);
                }

                return await Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Razorpay signature");
                return false;
            }
        }

        private string GenerateSignature(string orderId, string paymentId)
        {
            var message = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public Task<Dictionary<string, object>> CapturePaymentAsync(string razorpayPaymentId, decimal amount)
        {
            return Task.FromResult(new Dictionary<string, object>
            {
                { "success", true },
                { "payment_id", razorpayPaymentId }
            });
        }

        public Task<Dictionary<string, object>> RefundPaymentAsync(string razorpayPaymentId, decimal amount)
        {
            if (!IsConfigured)
            {
                return Task.FromResult(new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", "Razorpay is not configured." }
                });
            }

            try
            {
                var client = new RazorpayClient(_keyId, _keySecret);
                var amountInPaise = (long)(amount * 100);
                var refund = client.Refund.Create(new Dictionary<string, object> { { "amount", amountInPaise } });
                return Task.FromResult(new Dictionary<string, object>
                {
                    { "success", true },
                    { "refund_id", refund["id"] }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment {PaymentId}", razorpayPaymentId);
                return Task.FromResult(new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", ex.Message }
                });
            }
        }
    }
}
