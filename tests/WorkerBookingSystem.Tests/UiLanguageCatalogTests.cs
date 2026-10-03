using System.Globalization;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class UiLanguageCatalogTests
{
    [Fact]
    public void All_supported_indian_languages_have_valid_cultures_and_unique_codes()
    {
        Assert.Equal(23, UiLanguageCatalog.All.Count);
        Assert.Equal(UiLanguageCatalog.All.Count, UiLanguageCatalog.All.Select(language => language.Culture).Distinct().Count());

        foreach (var language in UiLanguageCatalog.All)
            Assert.Equal(language.Culture, CultureInfo.GetCultureInfo(language.Culture).Name);
    }

    [Theory]
    [InlineData("en-IN")]
    [InlineData("hi-IN")]
    [InlineData("ta-IN")]
    public void Supported_cultures_are_accepted(string culture)
    {
        Assert.True(UiLanguageCatalog.IsSupported(culture));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("not-a-culture")]
    [InlineData(null)]
    public void Unsupported_cultures_are_rejected(string? culture)
    {
        Assert.False(UiLanguageCatalog.IsSupported(culture));
    }
}
