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

    /// <summary>
    /// Client review by worker (feedback on client)
    /// </summary>
    public class ClientReview
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public int WorkerId { get; set; }

        public int? BookingId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        // Metrics
        public int Responsiveness { get; set; } = 3; // 1-5
        public int PaymentPunctuality { get; set; } = 3; // 1-5
        public int Cooperation { get; set; } = 3; // 1-5

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public bool IsVisible { get; set; } = true;
        public string AdminNotes { get; set; }

        public Client Client { get; set; }
        public Worker Worker { get; set; }
        public Booking Booking { get; set; }
    }
}

