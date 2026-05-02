namespace WorkerBookingSystem.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int WorkerId { get; set; }
        public int ClientId { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? TaskDescription { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public decimal TotalWage { get; set; } // Calculated based on hourly rate
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public Worker? Worker { get; set; }
        public Client? Client { get; set; }
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled
    }
}
