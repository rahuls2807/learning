using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public class Worker
    {
        public int WorkerId { get; set; }
        public string? UserId { get; set; } // Link to AspNetUser
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        [Required]
        public string? PhoneNumber { get; set; }
        public string? Skill { get; set; } // e.g., Plumbing, Electrical, etc.
        public string? ProfileImagePath { get; set; }
        public string? ResumePath { get; set; }
        public string? PreferredPayoutMethod { get; set; } = "UPI";
        public string? UpiId { get; set; }
        public string? BankAccountHolderName { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? IfscCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<WorkerAvailability> Availabilities { get; set; } = new List<WorkerAvailability>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<WorkerReview> Reviews { get; set; } = new List<WorkerReview>();
    }
}
