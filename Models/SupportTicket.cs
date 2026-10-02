using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public enum SupportTicketStatus
    {
        Open,
        InProgress,
        WaitingForCustomer,
        Resolved,
        Closed
    }

    public enum SupportTicketCategory
    {
        Booking,
        Payment,
        Account,
        Safety,
        Other
    }

    public class SupportTicket
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Subject { get; set; } = string.Empty;

        public SupportTicketCategory Category { get; set; } = SupportTicketCategory.Other;
        public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public string? AssignedToUserId { get; set; }
        public int? BookingId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ApplicationUser? CreatedByUser { get; set; }
        public ApplicationUser? AssignedToUser { get; set; }
        public Booking? Booking { get; set; }
        public ICollection<SupportTicketReply> Replies { get; set; } = new List<SupportTicketReply>();
    }

    public class SupportTicketReply
    {
        public int Id { get; set; }
        public int SupportTicketId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(4000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public SupportTicket? SupportTicket { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
