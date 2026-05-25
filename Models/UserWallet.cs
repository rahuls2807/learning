using System;

namespace WorkerBookingSystem.Models
{
    /// <summary>
    /// Digital wallet for faster payments and prepaid bookings
    /// </summary>
    public class UserWallet
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Balance in rupees
        public decimal BalanceAmount { get; set; }

        // Total recharged
        public decimal TotalRecharged { get; set; }

        // Total used
        public decimal TotalUsed { get; set; }

        // Loyalty points/cashback
        public int LoyaltyPoints { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Track wallet transactions (recharge, debit, cashback)
    /// </summary>
    public class WalletTransaction
    {
        public int Id { get; set; }

        public int WalletId { get; set; }
        public UserWallet Wallet { get; set; }

        // CREDIT or DEBIT
        public string Type { get; set; }

        // RECHARGE, BOOKING_PAYMENT, REFUND, CASHBACK, REFERRAL_BONUS
        public string TransactionType { get; set; }

        public decimal Amount { get; set; }

        // Opening balance before transaction
        public decimal OpeningBalance { get; set; }

        // Closing balance after transaction
        public decimal ClosingBalance { get; set; }

        public string Description { get; set; }

        public int? BookingId { get; set; }
        public Booking Booking { get; set; }

        // PENDING, SUCCESS, FAILED
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        // Payment gateway reference
        public string GatewayReference { get; set; }
    }
}
