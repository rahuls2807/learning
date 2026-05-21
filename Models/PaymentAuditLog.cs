using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    /// <summary>
    /// RBI Compliance: Immutable audit log for all payment transactions
    /// Retained for 5 years as per RBI Payment Systems Regulations
    /// </summary>
    public class PaymentAuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TransactionId { get; set; } = string.Empty; // Razorpay Order ID

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = "Card"; // Card, UPI, Netbanking, Wallet

        [Required]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = string.Empty; // Initiated, Verified, Failed, Refunded

        [Required]
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        [StringLength(500)]
        public string? GatewayResponse { get; set; }

        [StringLength(45)]
        public string? ClientIpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// For tamper-proofing - hash of previous record
        /// </summary>
        [StringLength(256)]
        public string? PreviousRecordHash { get; set; }

        // Navigation properties
        public Booking? Booking { get; set; }
    }

    /// <summary>
    /// OTP Verification for 2FA compliance
    /// </summary>
    public class OtpVerification
    {
        [Key]
        public int OtpId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; }

        public bool IsVerified { get; set; } = false;

        public int AttemptsRemaining { get; set; } = 3;

        // Navigation properties
        public Booking? Booking { get; set; }
    }

    /// <summary>
    /// Razorpay Order Mapping - tracks payment lifecycle
    /// </summary>
    public class RazorpayOrder
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string RazorpayOrderId { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? RazorpayPaymentId { get; set; }

        [StringLength(256)]
        public string? RazorpaySignature { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "created"; // created, authorized, captured, failed, refunded

        public DateTime? PaidAt { get; set; }

        [StringLength(500)]
        public string? ErrorDescription { get; set; }

        // Navigation properties
        public Booking? Booking { get; set; }
    }
}
