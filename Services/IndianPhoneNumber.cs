namespace WorkerBookingSystem.Services;

public static class IndianPhoneNumber
{
    public static string ToNationalDigits(string? phoneNumber)
    {
        var digits = new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
            return digits[2..];
        if (digits.Length == 11 && digits.StartsWith('0'))
            return digits[1..];
        return digits;
    }

    public static string ToE164(string? phoneNumber)
    {
        var nationalDigits = ToNationalDigits(phoneNumber);
        return nationalDigits.Length == 10 ? $"+91{nationalDigits}" : phoneNumber?.Trim() ?? string.Empty;
    }
}
