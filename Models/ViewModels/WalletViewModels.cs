using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class WalletDashboardViewModel
    {
        public UserWallet Wallet { get; set; } = new();
        public IReadOnlyList<WalletTransaction> RecentTransactions { get; set; } = Array.Empty<WalletTransaction>();
        public IReadOnlyList<WalletRecipientViewModel> RecentRecipients { get; set; } = Array.Empty<WalletRecipientViewModel>();
        public decimal MonthlySpending { get; set; }
        public decimal MonthlyCredits { get; set; }
        public int SuccessfulTransfers { get; set; }
        public decimal ProtectedFunds { get; set; }
    }

    public class WalletRecipientViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
    }
}
