using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class ClientBookingStatusViewModel
    {
        public int BookingId { get; set; }

        [Required]
        public BookingStatus Status { get; set; }

        [StringLength(300)]
        [Display(Name = "Note")]
        public string? ClientStatusNote { get; set; }
    }

    public class ClientCashPaymentViewModel
    {
        public int BookingId { get; set; }

        [Range(0.01, 999999)]
        [Display(Name = "Cash Paid to Worker")]
        public decimal AmountPaidToWorker { get; set; }

        [StringLength(300)]
        [Display(Name = "Note")]
        public string? ClientStatusNote { get; set; }
    }
}
