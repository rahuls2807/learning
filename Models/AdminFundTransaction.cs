using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public class AdminFundTransaction
    {
        public int AdminFundTransactionId { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        [StringLength(30)]
        public string TransactionType { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Direction { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [Required]
        [StringLength(40)]
        public string FundingSource { get; set; } = string.Empty;

        [StringLength(40)]
        public string Method { get; set; } = string.Empty;

        [StringLength(120)]
        public string Reference { get; set; } = string.Empty;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        public string AdminUserId { get; set; } = string.Empty;

        public ApplicationUser? AdminUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class AdminFundTransactionTypes
    {
        public const string ClientReceipt = "CLIENT_RECEIPT";
        public const string WorkerPayout = "WORKER_PAYOUT";
        public const string ClientAdjustment = "CLIENT_ADJUSTMENT";
    }

    public static class AdminFundDirections
    {
        public const string In = "IN";
        public const string Out = "OUT";
    }

    public static class FundingSources
    {
        public const string ClientReceivedFunds = "CLIENT_RECEIVED_FUNDS";
        public const string CompanyFundAdvance = "COMPANY_FUND_ADVANCE";
    }
}
