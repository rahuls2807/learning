using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    public static class BookingParticipantPolicy
    {
        public static bool CanAccess(Booking booking, string? userId, string? email = null)
        {
            return IsClient(booking, userId, email) || IsWorker(booking, userId, email);
        }

        public static bool IsClient(Booking booking, string? userId, string? email = null)
        {
            return Matches(booking.Client?.UserId, booking.Client?.Email, userId, email);
        }

        public static bool IsWorker(Booking booking, string? userId, string? email = null)
        {
            return Matches(booking.Worker?.UserId, booking.Worker?.Email, userId, email);
        }

        public static (string? UserId, string? Email) GetOtherParticipant(Booking booking, string? userId, string? email = null)
        {
            if (IsClient(booking, userId, email))
            {
                return (booking.Worker?.UserId, booking.Worker?.Email);
            }

            if (IsWorker(booking, userId, email))
            {
                return (booking.Client?.UserId, booking.Client?.Email);
            }

            return (null, null);
        }

        private static bool Matches(string? participantId, string? participantEmail, string? userId, string? email)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            return participantId == userId
                || (string.IsNullOrWhiteSpace(participantId)
                    && !string.IsNullOrWhiteSpace(email)
                    && !string.IsNullOrWhiteSpace(participantEmail)
                    && string.Equals(participantEmail, email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
