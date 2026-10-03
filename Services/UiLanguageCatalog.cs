using System.Globalization;

namespace WorkerBookingSystem.Services;

public sealed record UiLanguage(string Culture, string NativeName);

public static class UiLanguageCatalog
{
    public static IReadOnlyList<UiLanguage> All { get; } =
    [
        new("as-IN", "অসমীয়া"),
        new("bn-IN", "বাংলা"),
        new("brx-IN", "बड़ो"),
        new("doi-IN", "डोगरी"),
        new("en-IN", "English"),
        new("gu-IN", "ગુજરાતી"),
        new("hi-IN", "हिन्दी"),
        new("kn-IN", "ಕನ್ನಡ"),
        new("ks-IN", "کٲشُر"),
        new("kok-IN", "कोंकणी"),
        new("mai-IN", "मैथिली"),
        new("ml-IN", "മലയാളം"),
        new("mni-IN", "মৈতৈলোন্"),
        new("mr-IN", "मराठी"),
        new("ne-IN", "नेपाली"),
        new("or-IN", "ଓଡ଼ିଆ"),
        new("pa-IN", "ਪੰਜਾਬੀ"),
        new("sa-IN", "संस्कृतम्"),
        new("sat-IN", "ᱥᱟᱱᱛᱟᱲᱤ"),
        new("sd-IN", "سنڌي"),
        new("ta-IN", "தமிழ்"),
        new("te-IN", "తెలుగు"),
        new("ur-IN", "اردو")
    ];

    public static bool IsSupported(string? culture) =>
        All.Any(language => string.Equals(language.Culture, culture, StringComparison.OrdinalIgnoreCase));

    public static UiLanguage GetCurrent(CultureInfo culture) =>
        All.FirstOrDefault(language => string.Equals(language.Culture, culture.Name, StringComparison.OrdinalIgnoreCase))
        ?? All.First(language => language.Culture == "en-IN");
}
