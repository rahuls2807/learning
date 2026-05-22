namespace WorkerBookingSystem.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int WorkerId { get; set; }
        public string? ClientId { get; set; }
        public int? BookingId { get; set; }
        public int Rating { get; set; } // 1-5 stars
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public Worker? Worker { get; set; }
        public ApplicationUser? Client { get; set; }
        public Booking? Booking { get; set; }
    }
}
