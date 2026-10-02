using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class BookingParticipantPolicyTests
{
    private static Booking CreateBooking() => new()
    {
        Client = new Client { UserId = "client-user" },
        Worker = new Worker { UserId = "worker-user", PhoneNumber = "0000000000" }
    };

    [Theory]
    [InlineData("client-user", false, true)]
    [InlineData("worker-user", false, true)]
    [InlineData("other-user", false, false)]
    [InlineData(null, false, false)]
    [InlineData("other-user", true, true)]
    public void CanAccess_only_allows_participants_or_admin(string? userId, bool isAdmin, bool expected)
    {
        Assert.Equal(expected, BookingParticipantPolicy.CanAccess(CreateBooking(), userId, isAdmin));
    }

    [Theory]
    [InlineData("client-user", "worker-user")]
    [InlineData("worker-user", "client-user")]
    [InlineData("outsider", null)]
    [InlineData(null, null)]
    public void GetOtherParticipantId_returns_only_booking_counterparty(string? userId, string? expected)
    {
        Assert.Equal(expected, BookingParticipantPolicy.GetOtherParticipantId(CreateBooking(), userId));
    }
}
