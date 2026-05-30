namespace WorkerBookingSystem.Services.Sms
{
    public interface ISmsOtpSender
    {
        string ProviderName { get; }
        int Priority { get; }
        bool IsConfigured { get; }
        Task<(bool success, string? error)> SendOtpAsync(string normalizedPhone, string otp, int bookingId);
    }
}
