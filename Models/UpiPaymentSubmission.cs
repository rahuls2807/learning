using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public class UpiPaymentSubmission
    {
        public int UpiPaymentId { get; set; }

        public int BookingId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ClientUpiId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TransactionReference { get; set; } = string.Empty;

        [Range(0.01, 999999)]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string MerchantUpiId { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = UpiPaymentStatuses.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        public string? AdminNotes { get; set; }

        public Booking? Booking { get; set; }
    }

    public static class UpiPaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}
