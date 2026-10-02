using System.ComponentModel.DataAnnotations;
using WorkerBookingSystem.Models.ViewModels;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class WorkerProfessionalProfileTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void Experience_years_must_be_between_zero_and_sixty(int years)
    {
        var model = CreateValidEditModel();
        model.YearsExperience = years;

        Assert.Contains(Validate(model), error => error.MemberNames.Contains(nameof(model.YearsExperience)));
    }

    [Fact]
    public void Valid_professional_profile_fields_pass_validation()
    {
        var model = CreateValidEditModel();
        model.ProfessionalSummary = new string('a', 1500);
        model.YearsExperience = 15;
        model.Certifications = new string('c', 2000);

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Professional_summary_and_certifications_have_storage_safe_limits()
    {
        var model = CreateValidEditModel();
        model.ProfessionalSummary = new string('a', 1501);
        model.Certifications = new string('c', 2001);

        var errors = Validate(model);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.ProfessionalSummary)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(model.Certifications)));
    }

    private static WorkerEditViewModel CreateValidEditModel() => new()
    {
        FirstName = "Ravi",
        LastName = "Worker",
        Email = "ravi@example.test",
        PhoneNumber = "9876543210",
        Skill = "Electrician"
    };

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
