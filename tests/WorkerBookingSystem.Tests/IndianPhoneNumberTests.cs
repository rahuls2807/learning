using System.ComponentModel.DataAnnotations;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class IndianPhoneNumberTests
{
    [Theory]
    [InlineData("9876543210", "+919876543210")]
    [InlineData("+919876543210", "+919876543210")]
    [InlineData("09876543210", "+919876543210")]
    public void Phone_number_formats_to_india_e164(string input, string expected)
    {
        Assert.Equal(expected, IndianPhoneNumber.ToE164(input));
    }

    [Fact]
    public void Edit_field_gets_ten_national_digits_from_existing_india_format()
    {
        Assert.Equal("9876543210", IndianPhoneNumber.ToNationalDigits("+919876543210"));
    }

    [Theory]
    [InlineData("9876543210", true)]
    [InlineData("5876543210", false)]
    [InlineData("98765432101", false)]
    [InlineData("98765abc10", false)]
    [InlineData("987654321", false)]
    public void Signup_phone_field_requires_exactly_ten_valid_mobile_digits(string phone, bool expected)
    {
        Assert.Equal(expected, IsPhoneValid(typeof(ClientRegisterViewModel), phone));
        Assert.Equal(expected, IsPhoneValid(typeof(WorkerRegisterViewModel), phone));
        Assert.Equal(expected, IsPhoneValid(typeof(WorkerEditViewModel), phone));
    }

    [Fact]
    public void Otp_request_rejects_numbers_longer_than_ten_digits()
    {
        Assert.False(IsPhoneValid(typeof(OtpRequestViewModel), "98765432101"));
    }

    private static bool IsPhoneValid(Type modelType, string phone)
    {
        var model = Activator.CreateInstance(modelType)!;
        var results = new List<ValidationResult>();
        return Validator.TryValidateProperty(phone,
            new ValidationContext(model) { MemberName = "PhoneNumber" }, results);
    }
}
