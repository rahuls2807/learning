namespace WorkerBookingSystem.Models
{
    public class Worker
    {
        public int WorkerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Skill { get; set; } // e.g., Plumbing, Electrical, etc.
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<WorkerAvailability> Availabilities { get; set; } = new List<WorkerAvailability>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
