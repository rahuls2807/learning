using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class CompanyBankDetailsViewModel
    {
        [Display(Name = "Company / Merchant Name")]
        [MaxLength(120)]
        public string MerchantName { get; set; } = "Indian Worker Mandi";

        [Display(Name = "UPI ID")]
        [MaxLength(100)]
        public string MerchantUpiId { get; set; } = "rsinghrahul402@ybl";

        [Display(Name = "Account Holder Name")]
        [MaxLength(120)]
        public string AccountHolderName { get; set; } = string.Empty;

        [Display(Name = "Bank Name")]
        [MaxLength(120)]
        public string BankName { get; set; } = string.Empty;

        [Display(Name = "Account Number")]
        [MaxLength(60)]
        public string AccountNumber { get; set; } = string.Empty;

        [Display(Name = "IFSC Code")]
        [MaxLength(20)]
        public string IfscCode { get; set; } = string.Empty;

        [Display(Name = "Branch")]
        [MaxLength(120)]
        public string Branch { get; set; } = string.Empty;

        [Display(Name = "Payment Instructions")]
        [MaxLength(500)]
        public string PaymentInstructions { get; set; } = "Please include booking ID in the payment note and submit the UTR/reference after payment.";
    }
}
