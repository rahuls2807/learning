using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class AdminFundsDashboardViewModel
    {
        public IReadOnlyList<AdminFundBookingRowViewModel> Bookings { get; set; } = Array.Empty<AdminFundBookingRowViewModel>();
        public IReadOnlyList<AdminFundTransaction> RecentTransactions { get; set; } = Array.Empty<AdminFundTransaction>();
        public decimal TotalClientReceived { get; set; }
        public decimal TotalWorkerPaid { get; set; }
        public decimal TotalCompanyAdvanced { get; set; }
        public decimal TotalClientOutstanding { get; set; }
        public decimal TotalRecoverableFromClient { get; set; }
    }

    public class AdminFundBookingRowViewModel
    {
        public int BookingId { get; set; }
        public string ClientName { get; set; } = "Walk-in / Admin";
        public string WorkerName { get; set; } = string.Empty;
        public string WorkerSkill { get; set; } = string.Empty;
        public decimal TotalWage { get; set; }
        public decimal ClientReceived { get; set; }
        public decimal WorkerPaid { get; set; }
        public decimal CompanyAdvanced { get; set; }
        public decimal ClientDue { get; set; }
        public decimal WorkerDue { get; set; }
        public decimal AdvanceRecoverable { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
        public string WorkerPreferredPayoutMethod { get; set; } = string.Empty;
        public string WorkerUpiId { get; set; } = string.Empty;
        public string WorkerBankSummary { get; set; } = string.Empty;
    }

    public class RecordClientReceiptViewModel
    {
        public int BookingId { get; set; }

        [Range(0.01, 999999)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(40)]
        public string Method { get; set; } = "UPI";

        [MaxLength(120)]
        public string? Reference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class RecordWorkerPayoutViewModel
    {
        public int BookingId { get; set; }

        [Range(0.01, 999999)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(40)]
        public string FundingSource { get; set; } = FundingSources.ClientReceivedFunds;

        [Required]
        [MaxLength(40)]
        public string Method { get; set; } = "UPI";

        [MaxLength(120)]
        public string? Reference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
