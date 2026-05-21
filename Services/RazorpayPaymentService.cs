using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace WorkerBookingSystem.Services
{
    /// <summary>
    /// RBI-Compliant Razorpay Payment Gateway Service
    /// Handles tokenized payments without storing card data
    /// </summary>
    public interface IRazorpayPaymentService
    {
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

        public RazorpayPaymentService(IConfiguration configuration, ILogger<RazorpayPaymentService> logger)
        {
            _keyId = configuration["Razorpay:KeyId"] ?? throw new InvalidOperationException("Razorpay KeyId not configured");
            _keySecret = configuration["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret not configured");
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> CreateOrderAsync(int bookingId, decimal amount, string clientEmail, string clientPhone)
        {
            try
            {
                var client = new RazorpayClient(_keyId, _keySecret);
                
                // Amount in paise (smallest unit for INR)
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
                
                _logger.LogInformation($"Razorpay order created: {order["id"]} for booking {bookingId}");

                return new Dictionary<string, object>
                {
                    { "order_id", order["id"] },
                    { "amount", amountInPaise },
                    { "key_id", _keyId },
                    { "success", true }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Razorpay order: {ex.Message}");
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }

        /// <summary>
        /// Verify payment signature - CRITICAL for security
        /// Confirms payment came from Razorpay and was not tampered
        /// </summary>
        public async Task<bool> VerifyPaymentSignatureAsync(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            try
            {
                var expectedSignature = GenerateSignature(razorpayOrderId, razorpayPaymentId);
                
                var isValid = expectedSignature == razorpaySignature;
                
                if (!isValid)
                {
                    _logger.LogWarning($"Signature verification failed for payment {razorpayPaymentId}");
                }
                else
                {
                    _logger.LogInformation($"Payment signature verified for {razorpayPaymentId}");
                }

                return await Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying signature: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate HMAC SHA256 signature
        /// </summary>
        private string GenerateSignature(string orderId, string paymentId)
        {
            var message = $"{orderId}|{paymentId}";
            
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public async Task<Dictionary<string, object>> CapturePaymentAsync(string razorpayPaymentId, decimal amount)
        {
            try
            {
                var client = new RazorpayClient(_keyId, _keySecret);
                var amountInPaise = (long)(amount * 100);

                var options = new Dictionary<string, object>
                {
                    { "amount", amountInPaise }
                };

                var payment = client.Payment.Capture(razorpayPaymentId, amountInPaise);
                
                _logger.LogInformation($"Payment captured: {razorpayPaymentId}");

                return new Dictionary<string, object>
                {
                    { "success", true },
                    { "payment_id", payment["id"] }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing payment: {ex.Message}");
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }

        public async Task<Dictionary<string, object>> RefundPaymentAsync(string razorpayPaymentId, decimal amount)
        {
            try
            {
                var client = new RazorpayClient(_keyId, _keySecret);
                var amountInPaise = (long)(amount * 100);

                var options = new Dictionary<string, object>
                {
                    { "amount", amountInPaise }
                };

                var refund = client.Payment.Refund(razorpayPaymentId, amountInPaise);
                
                _logger.LogInformation($"Refund processed: {razorpayPaymentId}");

                return new Dictionary<string, object>
                {
                    { "success", true },
                    { "refund_id", refund["id"] }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refunding payment: {ex.Message}");
                return new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }
    }
}
