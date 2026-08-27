using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class ClientRegisterViewModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class WorkerRegisterViewModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Skill { get; set; } = string.Empty;

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Resume")]
        public IFormFile? Resume { get; set; }

        [Display(Name = "Preferred Payout Method")]
        public string PreferredPayoutMethod { get; set; } = "UPI";

        [Display(Name = "UPI ID")]
        [MaxLength(100)]
        public string? UpiId { get; set; }

        [Display(Name = "Account Holder Name")]
        [MaxLength(120)]
        public string? BankAccountHolderName { get; set; }

        [Display(Name = "Bank Name")]
        [MaxLength(120)]
        public string? BankName { get; set; }

        [Display(Name = "Account Number")]
        [MaxLength(60)]
        public string? BankAccountNumber { get; set; }

        [Display(Name = "IFSC Code")]
        [MaxLength(20)]
        public string? IfscCode { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class WorkerEditViewModel
    {
        public int WorkerId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Skill { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string? CurrentProfileImagePath { get; set; }
        public string? CurrentResumePath { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Resume")]
        public IFormFile? Resume { get; set; }

        [Display(Name = "Preferred Payout Method")]
        public string PreferredPayoutMethod { get; set; } = "UPI";

        [Display(Name = "UPI ID")]
        [MaxLength(100)]
        public string? UpiId { get; set; }

        [Display(Name = "Account Holder Name")]
        [MaxLength(120)]
        public string? BankAccountHolderName { get; set; }

        [Display(Name = "Bank Name")]
        [MaxLength(120)]
        public string? BankName { get; set; }

        [Display(Name = "Account Number")]
        [MaxLength(60)]
        public string? BankAccountNumber { get; set; }

        [Display(Name = "IFSC Code")]
        [MaxLength(20)]
        public string? IfscCode { get; set; }
    }

    public class AdminRegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
