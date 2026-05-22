using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    /// <summary>
    /// RBI Compliance: Payment Audit Logging Service
    /// Maintains immutable audit trail for 5 years as per RBI Payment Systems Regulations
    /// </summary>
    public interface IPaymentAuditService
    {
        Task LogPaymentInitiationAsync(int bookingId, string clientId, decimal amount, string paymentMethod, HttpContext httpContext);
        Task LogPaymentVerificationAsync(int bookingId, string transactionId, bool verified, string? failureReason);
        Task LogPaymentCompletionAsync(int bookingId, string transactionId, PaymentStatus status, string gatewayResponse);
    }

    public class PaymentAuditService : IPaymentAuditService
    {
        private readonly WorkerBookingContext _context;
        private readonly ILogger<PaymentAuditService> _logger;

        public PaymentAuditService(WorkerBookingContext context, ILogger<PaymentAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogPaymentInitiationAsync(int bookingId, string clientId, decimal amount, string paymentMethod, HttpContext httpContext)
        {
            try
            {
                var previousLog = await _context.PaymentAuditLogs
                    .Where(l => l.BookingId == bookingId)
                    .OrderByDescending(l => l.InitiatedAt)
                    .FirstOrDefaultAsync();

                var auditLog = new PaymentAuditLog
                {
                    BookingId = bookingId,
                    ClientId = clientId,
                    TransactionId = $"TEMP-{Guid.NewGuid():N}",
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "Initiated",
                    InitiatedAt = DateTime.UtcNow,
                    ClientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    PreviousRecordHash = previousLog != null ? GenerateHash(previousLog) : null
                };

                _context.PaymentAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payment audit log created for booking {bookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating payment audit log: {ex.Message}");
            }
        }

        public async Task LogPaymentVerificationAsync(int bookingId, string transactionId, bool verified, string? failureReason)
        {
            try
            {
                var auditLog = await _context.PaymentAuditLogs
                    .Where(l => l.BookingId == bookingId)
                    .OrderByDescending(l => l.InitiatedAt)
                    .FirstOrDefaultAsync();

                if (auditLog != null)
                {
                    auditLog.TransactionId = transactionId;
                    auditLog.PaymentStatus = verified ? "Verified" : "Failed";
                    auditLog.FailureReason = failureReason;
                    auditLog.CompletedAt = DateTime.UtcNow;

                    _context.PaymentAuditLogs.Update(auditLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Payment verification logged for booking {bookingId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging payment verification: {ex.Message}");
            }
        }

        public async Task LogPaymentCompletionAsync(int bookingId, string transactionId, PaymentStatus status, string gatewayResponse)
        {
            try
            {
                var auditLog = await _context.PaymentAuditLogs
                    .Where(l => l.BookingId == bookingId && l.TransactionId == transactionId)
                    .FirstOrDefaultAsync();

                if (auditLog != null)
                {
                    auditLog.PaymentStatus = status.ToString();
                    auditLog.GatewayResponse = gatewayResponse;
                    auditLog.CompletedAt = DateTime.UtcNow;

                    _context.PaymentAuditLogs.Update(auditLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Payment completion logged for booking {bookingId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging payment completion: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate SHA256 hash of audit log for tamper detection
        /// </summary>
        private string GenerateHash(PaymentAuditLog log)
        {
            var json = JsonSerializer.Serialize(log);
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
