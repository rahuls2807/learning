using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    public static class BookingParticipantPolicy
    {
        public static bool CanAccess(Booking booking, string? userId, bool isAdmin = false)
        {
            if (isAdmin)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            return booking.Worker?.UserId == userId || booking.Client?.UserId == userId;
        }

        public static string? GetOtherParticipantId(Booking booking, string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            if (booking.Client?.UserId == userId)
            {
                return booking.Worker?.UserId;
            }

            if (booking.Worker?.UserId == userId)
            {
                return booking.Client?.UserId;
            }

            return null;
        }
    }
}
