namespace WorkerBookingSystem.Services;

public static class PaymentMethodCatalog
{
    public static string[] GetAvailable(bool razorpayConfigured)
    {
        return razorpayConfigured
            ? ["UPI apps (PhonePe, Google Pay, Paytm)", "Cards / Net Banking / Wallet", "Manual UPI QR"]
            : ["Manual UPI QR"];
    }
}
