namespace WorkerBookingSystem.Services
{
    public static class WorkerProfileMessagePolicy
    {
        public static bool CanMessage(
            bool isAuthenticated,
            bool isClient,
            string? currentUserId,
            string? workerUserId,
            bool workerIsActive)
        {
            return isAuthenticated
                && isClient
                && workerIsActive
                && !string.IsNullOrWhiteSpace(currentUserId)
                && !string.IsNullOrWhiteSpace(workerUserId)
                && !string.Equals(currentUserId, workerUserId, StringComparison.Ordinal);
        }
    }
}
