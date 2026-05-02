namespace WorkerBookingSystem.Models
{
    public class HourlyRate
    {
        public int RateId { get; set; }
        public int WorkerId { get; set; }
        public string? Skill { get; set; } // e.g., Plumbing, Electrical
        public decimal RatePerHour { get; set; }
        public DateTime EffectiveDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation property
        public Worker? Worker { get; set; }
    }
}
