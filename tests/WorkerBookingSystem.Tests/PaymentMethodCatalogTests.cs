using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class PaymentMethodCatalogTests
{
    [Fact]
    public void Manual_upi_is_available_without_gateway_configuration()
    {
        Assert.Contains("Manual UPI QR", PaymentMethodCatalog.GetAvailable(false));
        Assert.DoesNotContain("Cards / Net Banking / Wallet", PaymentMethodCatalog.GetAvailable(false));
    }

    [Fact]
    public void Upi_and_card_methods_are_available_when_gateway_is_configured()
    {
        var methods = PaymentMethodCatalog.GetAvailable(true);

        Assert.Contains("UPI apps (PhonePe, Google Pay, Paytm)", methods);
        Assert.Contains("Cards / Net Banking / Wallet", methods);
        Assert.Contains("Manual UPI QR", methods);
    }
}
