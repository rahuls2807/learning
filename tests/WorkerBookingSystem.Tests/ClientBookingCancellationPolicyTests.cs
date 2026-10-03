using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class ClientBookingCancellationPolicyTests
{
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void Unpaid_upcoming_bookings_can_be_cancelled(BookingStatus status)
    {
        Assert.True(ClientBookingCancellationPolicy.CanCancel(new Booking { Status = status }));
    }

    [Theory]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public void Started_or_closed_bookings_cannot_be_cancelled_again(BookingStatus status)
    {
        Assert.False(ClientBookingCancellationPolicy.CanCancel(new Booking { Status = status }));
    }

    [Fact]
    public void A_booking_with_any_payment_requires_support_to_cancel()
    {
        Assert.False(ClientBookingCancellationPolicy.CanCancel(new Booking
        {
            Status = BookingStatus.Confirmed,
            AmountPaidOnline = 1
        }));
        Assert.False(ClientBookingCancellationPolicy.CanCancel(new Booking
        {
            Status = BookingStatus.Pending,
            AmountPaidToWorker = 1
        }));
    }
}
