namespace WorkerBookingSystem.Services;

public sealed record RoleLandingDestination(string Controller, string Action);

public static class RoleLandingPolicy
{
    public static RoleLandingDestination? GetDestination(bool isAdmin, bool isWorker, bool isClient)
    {
        if (isAdmin)
            return new RoleLandingDestination("Admin", "Dashboard");
        if (isWorker)
            return new RoleLandingDestination("Worker", "MyBookings");
        if (isClient)
            return new RoleLandingDestination("Client", "MyBookings");
        return null;
    }
}
