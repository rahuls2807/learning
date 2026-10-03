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

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AccountProfileViewModel
    {
        [Required, StringLength(80)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, Display(Name = "Mobile number")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Enter exactly 10 digits after +91.")]
        [RegularExpression("^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [StringLength(80)]
        public string City { get; set; } = string.Empty;

        [StringLength(80)]
        public string State { get; set; } = string.Empty;

        [Display(Name = "PIN code")]
        [RegularExpression("^$|^[1-9][0-9]{5}$", ErrorMessage = "Enter a valid 6-digit PIN code.")]
        public string PinCode { get; set; } = string.Empty;
    }

    public class ConfirmProfileEmailViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
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
        [Display(Name = "Mobile number")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Enter exactly 10 digits after +91.")]
        [RegularExpression("^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
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
        [Display(Name = "Mobile number")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Enter exactly 10 digits after +91.")]
        [RegularExpression("^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
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
        [Display(Name = "Mobile number")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Enter exactly 10 digits after +91.")]
        [RegularExpression("^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Skill { get; set; } = string.Empty;

        [Display(Name = "Professional summary")]
        [StringLength(1500)]
        public string? ProfessionalSummary { get; set; }

        [Display(Name = "Years of experience")]
        [Range(0, 60)]
        public int? YearsExperience { get; set; }

        [Display(Name = "Certificates and training")]
        [StringLength(2000)]
        public string? Certifications { get; set; }

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
