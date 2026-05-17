using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int BookingId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AlreadyPaidOnline { get; set; }
        public decimal AlreadyPaidToWorker { get; set; }
        public decimal BalanceDue { get; set; }

        [Range(0.01, 999999)]
        [Display(Name = "Amount to Pay Online")]
        public decimal OnlineAmount { get; set; }

        [Required]
        [CreditCard]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        [Display(Name = "Name on Card")]
        public string CardholderName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Use MM/YY format.")]
        [Display(Name = "Expiry")]
        public string Expiry { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Use the 3 or 4 digit security code.")]
        [Display(Name = "CVV")]
        public string Cvv { get; set; } = string.Empty;
    }
}
