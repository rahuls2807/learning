using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public class WorkerReview
    {
        public int WorkerReviewId { get; set; }

        [Required]
        public int WorkerId { get; set; }

        public int? ClientId { get; set; }

        public int? BookingId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        [Required]
        [StringLength(120)]
        public string ReviewerName { get; set; } = string.Empty;

        public bool IsAdminReview { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Worker? Worker { get; set; }
        public Client? Client { get; set; }
        public Booking? Booking { get; set; }
    }
}
