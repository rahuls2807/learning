namespace WorkerBookingSystem.Models
{
    public class WorkerAvailability
    {
        public int AvailabilityId { get; set; }
        public int WorkerId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; } = true;

        // Navigation property
        public Worker? Worker { get; set; }
    }
}
