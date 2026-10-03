namespace WorkerBookingSystem.Services;

public static class BestEffortDelivery
{
    public static async Task RunAsync(Func<Task> delivery, Action<Exception> onFailure)
    {
        try
        {
            await delivery();
        }
        catch (Exception exception)
        {
            onFailure(exception);
        }
    }
}
