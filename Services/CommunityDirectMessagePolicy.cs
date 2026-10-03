namespace WorkerBookingSystem.Services;

public static class CommunityDirectMessagePolicy
{
    public static bool CanStart(string? senderRole, string? recipientRole, bool hasBooking)
    {
        if (senderRole is not ("Admin" or "Client" or "Worker")
            || recipientRole is not ("Admin" or "Client" or "Worker"))
            return false;

        if (senderRole == "Admin" || recipientRole == "Admin")
            return true;

        return string.Equals(senderRole, recipientRole, StringComparison.Ordinal)
            || hasBooking;
    }
}
