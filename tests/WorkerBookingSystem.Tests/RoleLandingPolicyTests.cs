using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class RoleLandingPolicyTests
{
    [Theory]
    [InlineData(true, true, true, "Admin", "Dashboard")]
    [InlineData(false, true, false, "Worker", "MyBookings")]
    [InlineData(false, false, true, "Client", "MyBookings")]
    public void Each_role_gets_its_own_landing_workspace(
        bool admin, bool worker, bool client, string controller, string action)
    {
        Assert.Equal(new RoleLandingDestination(controller, action),
            RoleLandingPolicy.GetDestination(admin, worker, client));
    }

    [Fact]
    public void Anonymous_user_stays_on_public_home()
    {
        Assert.Null(RoleLandingPolicy.GetDestination(false, false, false));
    }
}
