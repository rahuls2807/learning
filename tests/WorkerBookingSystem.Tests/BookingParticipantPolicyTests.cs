using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;
using WorkerBookingSystem.Controllers;
using WorkerBookingSystem.Hubs;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class BookingParticipantPolicyTests
{
    private static Booking CreateBooking() => new()
    {
        Client = new Client { UserId = "client-user", Email = "client@example.test" },
        Worker = new Worker { UserId = "worker-user", Email = "worker@example.test", PhoneNumber = "0000000000" }
    };

    private static Booking CreateLegacyBooking() => new()
    {
        Client = new Client { Email = "client@example.test" },
        Worker = new Worker { Email = "worker@example.test", PhoneNumber = "0000000000" }
    };

    [Theory]
    [InlineData("client-user", true)]
    [InlineData("worker-user", true)]
    [InlineData("other-user", false)]
    [InlineData("admin-user", false)]
    [InlineData(null, false)]
    public void CanAccess_only_allows_the_booking_participants(string? userId, bool expected)
    {
        Assert.Equal(expected, BookingParticipantPolicy.CanAccess(CreateBooking(), userId));
    }

    [Fact]
    public void Email_fallback_does_not_override_a_profile_bound_to_another_account()
    {
        Assert.False(BookingParticipantPolicy.CanAccess(CreateBooking(), "other-user", "client@example.test"));
    }

    [Theory]
    [InlineData("legacy-client-user", "CLIENT@example.test", true)]
    [InlineData("legacy-worker-user", "worker@example.test", true)]
    [InlineData("other-user", "outsider@example.test", false)]
    public void CanAccess_uses_email_for_legacy_profiles_without_an_account_id(string? userId, string email, bool expected)
    {
        Assert.Equal(expected, BookingParticipantPolicy.CanAccess(CreateLegacyBooking(), userId, email));
    }

    [Theory]
    [InlineData("legacy-client-user", "client@example.test", null, "worker@example.test")]
    [InlineData("legacy-worker-user", "worker@example.test", null, "client@example.test")]
    [InlineData("outsider", null, null, null)]
    public void GetOtherParticipant_returns_only_the_booking_counterparty(string? userId, string? email, string? expectedId, string? expectedEmail)
    {
        Assert.Equal((expectedId, expectedEmail), BookingParticipantPolicy.GetOtherParticipant(CreateLegacyBooking(), userId, email));
    }

    [Fact]
    public void Booking_chat_requires_sign_in_without_restricting_users_to_admin_or_marketplace_roles()
    {
        var controllerAuthorization = typeof(ChatController).GetCustomAttribute<AuthorizeAttribute>();
        var hubAuthorization = typeof(BookingChatHub).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorization);
        Assert.Null(controllerAuthorization!.Roles);
        Assert.NotNull(hubAuthorization);
        Assert.Null(hubAuthorization!.Roles);
    }
}
