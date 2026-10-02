using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class WorkerProfileMessagePolicyTests
{
    [Fact]
    public void Client_can_message_profile_only_after_booking()
    {
        Assert.True(WorkerProfileMessagePolicy.CanMessage(true, true, false, "client-1", "worker-1", true, true));
        Assert.False(WorkerProfileMessagePolicy.CanMessage(true, true, false, "client-1", "worker-1", true, false));
    }

    [Fact]
    public void Worker_can_message_another_worker_from_profile()
    {
        Assert.True(WorkerProfileMessagePolicy.CanMessage(true, false, true, "worker-1", "worker-2", true, false));
    }

    [Theory]
    [InlineData(false, true, false, "client-1", "worker-1", true, true)]
    [InlineData(true, false, false, "admin-1", "worker-1", true, true)]
    [InlineData(true, true, false, "worker-1", "worker-1", true, true)]
    [InlineData(true, true, false, "client-1", null, true, true)]
    [InlineData(true, true, false, "client-1", "worker-1", false, true)]
    public void Profile_messaging_is_hidden_for_unauthorized_or_unavailable_accounts(
        bool isAuthenticated,
        bool isClient,
        bool isWorker,
        string? currentUserId,
        string? workerUserId,
        bool workerIsActive,
        bool hasBooking)
    {
        Assert.False(WorkerProfileMessagePolicy.CanMessage(
            isAuthenticated, isClient, isWorker, currentUserId, workerUserId, workerIsActive, hasBooking));
    }
}
