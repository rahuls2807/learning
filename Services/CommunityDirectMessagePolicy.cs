namespace WorkerBookingSystem.Services;

public static class CommunityDirectMessagePolicy
{
    public static bool CanStart(string? senderRole, string? recipientRole, bool hasBooking)
    {
        if (senderRole is not ("Client" or "Worker") || recipientRole is not ("Client" or "Worker"))
            return false;

        return string.Equals(senderRole, recipientRole, StringComparison.Ordinal)
            || hasBooking;
    }
}
