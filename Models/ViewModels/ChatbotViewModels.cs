using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class ChatbotRequest
    {
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Page { get; set; }
    }

    public class ChatbotResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool UsedAi { get; set; }
    }
}
