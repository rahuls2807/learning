using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class CommunityConversationPolicyTests
{
    [Fact]
    public void Pair_ordering_is_independent_of_who_starts_the_conversation()
    {
        Assert.Equal(
            CommunityConversationPolicy.OrderPair("worker-b", "client-a"),
            CommunityConversationPolicy.OrderPair("client-a", "worker-b"));
    }

    [Fact]
    public void Only_conversation_participants_can_access_direct_messages()
    {
        var conversation = new CommunityConversation { UserOneId = "client-a", UserTwoId = "worker-b" };

        Assert.True(CommunityConversationPolicy.CanAccess(conversation, "client-a"));
        Assert.True(CommunityConversationPolicy.CanAccess(conversation, "worker-b"));
        Assert.False(CommunityConversationPolicy.CanAccess(conversation, "outsider"));
        Assert.False(CommunityConversationPolicy.CanAccess(conversation, null));
    }

    [Theory]
    [InlineData("Client", "Client", false, true)]
    [InlineData("Worker", "Worker", false, true)]
    [InlineData("Client", "Worker", false, false)]
    [InlineData("Worker", "Client", true, true)]
    [InlineData("Admin", "Client", true, false)]
    public void Direct_messages_require_same_role_or_cross_role_booking(
        string senderRole, string recipientRole, bool hasBooking, bool expected)
    {
        Assert.Equal(expected, CommunityDirectMessagePolicy.CanStart(senderRole, recipientRole, hasBooking));
    }
}
