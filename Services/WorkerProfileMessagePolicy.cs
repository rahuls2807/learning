namespace WorkerBookingSystem.Services
{
    public static class WorkerProfileMessagePolicy
    {
        public static bool CanMessage(
            bool isAuthenticated,
            bool isClient,
            bool isWorker,
            string? currentUserId,
            string? workerUserId,
            bool workerIsActive,
            bool hasBooking)
        {
            return isAuthenticated
                && workerIsActive
                && !string.IsNullOrWhiteSpace(currentUserId)
                && !string.IsNullOrWhiteSpace(workerUserId)
                && !string.Equals(currentUserId, workerUserId, StringComparison.Ordinal)
                && ((isClient && hasBooking) || isWorker);
        }
    }
}
