using System.ComponentModel.DataAnnotations;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class AccountProfileTests
{
    [Fact]
    public void Profile_requires_a_valid_indian_mobile_number_and_email()
    {
        var profile = ValidProfile();
        profile.PhoneNumber = "98765432101";
        profile.Email = "not-an-email";

        var errors = Validate(profile);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(profile.PhoneNumber)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(profile.Email)));
    }

    [Fact]
    public void Profile_accepts_valid_contact_and_optional_pin_code()
    {
        var profile = ValidProfile();
        profile.PinCode = string.Empty;

        Assert.Empty(Validate(profile));
    }

    [Fact]
    public void Admin_display_name_uses_saved_profile_name_instead_of_email_prefix()
    {
        var displayName = CommunityProfilePolicy.GetDisplayName(new ApplicationUser
        {
            Email = "admin@example.test",
            BioDescription = "Admin Name"
        });

        Assert.Equal("Admin Name", displayName);
    }

    private static AccountProfileViewModel ValidProfile() => new()
    {
        FirstName = "Ravi",
        LastName = "Kumar",
        Email = "ravi@example.test",
        PhoneNumber = "9876543210",
        Address = "12 Main Road",
        City = "Pune",
        State = "Maharashtra",
        PinCode = "411001"
    };

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
