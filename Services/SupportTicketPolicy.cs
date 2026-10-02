using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    public static class SupportTicketPolicy
    {
        public static bool CanAccess(SupportTicket ticket, string? userId, bool isAdmin = false)
        {
            return isAdmin || (!string.IsNullOrWhiteSpace(userId) && ticket.CreatedByUserId == userId);
        }

        public static bool CanReply(SupportTicket ticket, string? userId, bool isAdmin = false)
        {
            return isAdmin || (CanAccess(ticket, userId) && ticket.Status != SupportTicketStatus.Closed);
        }

        public static SupportTicketStatus StatusAfterReply(SupportTicketStatus currentStatus, bool isAdmin)
        {
            if (isAdmin)
            {
                return SupportTicketStatus.WaitingForCustomer;
            }

            return currentStatus is SupportTicketStatus.Resolved or SupportTicketStatus.Closed
                ? SupportTicketStatus.Open
                : currentStatus;
        }
    }
}
