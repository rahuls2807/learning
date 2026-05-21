using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    /// <summary>
    /// RBI-Compliant Payment View Model
    /// No card data stored - uses tokenized Razorpay gateway
    /// </summary>
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

        // Razorpay fields (tokenized, no card storage)
        [Display(Name = "Razorpay Order ID")]
        public string? RazorpayOrderId { get; set; }

        [Display(Name = "Razorpay Payment ID")]
        public string? RazorpayPaymentId { get; set; }

        [Display(Name = "Razorpay Signature")]
        public string? RazorpaySignature { get; set; }

        // OTP Verification fields
        [Display(Name = "Phone Number for OTP")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "OTP Code")]
        [StringLength(6)]
        public string? OtpCode { get; set; }

        public bool OtpVerified { get; set; } = false;

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "card"; // card, upi, netbanking, wallet

        // Razorpay key for frontend
        public string? RazorpayKeyId { get; set; }
    }

    /// <summary>
    /// DTO for OTP Request
    /// </summary>
    public class OtpRequestViewModel
    {
        public int BookingId { get; set; }
        
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for OTP Verification
    /// </summary>
    public class OtpVerificationViewModel
    {
        public int BookingId { get; set; }
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }
}

