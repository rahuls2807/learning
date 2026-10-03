using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class BestEffortDeliveryTests
{
    [Fact]
    public async Task Realtime_failure_is_reported_without_failing_the_saved_send()
    {
        var deliveryError = new InvalidOperationException("Realtime transport unavailable");
        Exception? reported = null;

        await BestEffortDelivery.RunAsync(
            () => Task.FromException(deliveryError),
            exception => reported = exception);

        Assert.Same(deliveryError, reported);
    }

    [Fact]
    public async Task Successful_realtime_delivery_does_not_report_an_error()
    {
        Exception? reported = null;

        await BestEffortDelivery.RunAsync(
            () => Task.CompletedTask,
            exception => reported = exception);

        Assert.Null(reported);
    }
}
