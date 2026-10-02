using System.ComponentModel.DataAnnotations;
using WorkerBookingSystem.Models.ViewModels;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class AuthViewModelTests
{
    [Fact]
    public void Forgot_password_requires_a_valid_email_address()
    {
        Assert.False(IsValid(new ForgotPasswordViewModel { Email = "not-an-email" }));
        Assert.True(IsValid(new ForgotPasswordViewModel { Email = "member@example.com" }));
    }

    [Fact]
    public void Reset_password_requires_matching_password_confirmation_and_token()
    {
        var model = new ResetPasswordViewModel
        {
            Email = "member@example.com",
            Token = "identity-reset-token",
            Password = "new-password",
            ConfirmPassword = "different-password"
        };

        Assert.False(IsValid(model));
        model.ConfirmPassword = model.Password;
        Assert.True(IsValid(model));
    }

    [Fact]
    public void Change_password_requires_a_current_password_and_matching_new_password()
    {
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password",
            ConfirmPassword = "new-password"
        };

        Assert.True(IsValid(model));
        model.ConfirmPassword = "other-password";
        Assert.False(IsValid(model));
    }

    private static bool IsValid(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
    }
}
