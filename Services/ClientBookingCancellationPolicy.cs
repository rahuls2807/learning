using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services;

public static class ClientBookingCancellationPolicy
{
    public static bool CanCancel(Booking booking) =>
        booking.Status is BookingStatus.Pending or BookingStatus.Confirmed
        && booking.AmountPaidOnline == 0
        && booking.AmountPaidToWorker == 0;
}
