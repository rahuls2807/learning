using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public class Booking
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public int WorkerId { get; set; }
        [Required]
        public int ClientId { get; set; }
        [Required]
        public DateTime BookingDate { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        public string? TaskDescription { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public string? PaymentReference { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal AmountPaidOnline { get; set; }
        public decimal AmountPaidToWorker { get; set; }
        public string? ClientStatusNote { get; set; }
        public DateTime? LastClientStatusUpdate { get; set; }
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

    public enum PaymentStatus
    {
        Unpaid,
        Paid,
        Failed,
        Refunded,
        PartiallyPaid
    }
}
