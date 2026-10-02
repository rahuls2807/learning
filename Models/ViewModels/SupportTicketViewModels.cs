using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class CreateSupportTicketViewModel
    {
        [Required, StringLength(120, MinimumLength = 5)]
        public string Subject { get; set; } = string.Empty;

        public SupportTicketCategory Category { get; set; } = SupportTicketCategory.Other;
        public int? BookingId { get; set; }

        [Required, StringLength(4000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        public IEnumerable<SelectListItem> Bookings { get; set; } = Array.Empty<SelectListItem>();
    }

    public class ReplySupportTicketViewModel
    {
        public int TicketId { get; set; }

        [Required, StringLength(4000, MinimumLength = 2)]
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateSupportTicketViewModel
    {
        public int TicketId { get; set; }
        public SupportTicketStatus Status { get; set; }
    }

    public class BookingChatViewModel
    {
        public int BookingId { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public string OtherParticipantName { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    public class ChatConversationViewModel
    {
        public int BookingId { get; set; }
        public DateTime BookingDate { get; set; }
        public BookingStatus Status { get; set; }
        public string TaskDescription { get; set; } = string.Empty;
        public string OtherParticipantName { get; set; } = string.Empty;
    }
}
