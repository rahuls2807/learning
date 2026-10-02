using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class WorkerProfileMessagePolicyTests
{
    [Fact]
    public void Active_worker_can_be_messaged_from_profile_by_another_authenticated_client()
    {
        Assert.True(WorkerProfileMessagePolicy.CanMessage(true, true, "client-1", "worker-1", true));
    }

    [Theory]
    [InlineData(false, true, "client-1", "worker-1", true)]
    [InlineData(true, false, "admin-1", "worker-1", true)]
    [InlineData(true, true, "worker-1", "worker-1", true)]
    [InlineData(true, true, "client-1", null, true)]
    [InlineData(true, true, "client-1", "worker-1", false)]
    public void Profile_messaging_is_hidden_for_unauthorized_or_unavailable_accounts(
        bool isAuthenticated,
        bool isClient,
        string? currentUserId,
        string? workerUserId,
        bool workerIsActive)
    {
        Assert.False(WorkerProfileMessagePolicy.CanMessage(
            isAuthenticated, isClient, currentUserId, workerUserId, workerIsActive));
    }
}
