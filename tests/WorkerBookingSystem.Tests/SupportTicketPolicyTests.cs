using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class SupportTicketPolicyTests
{
    private static SupportTicket CreateTicket(SupportTicketStatus status = SupportTicketStatus.Open) => new()
    {
        CreatedByUserId = "ticket-owner",
        Status = status
    };

    [Fact]
    public void Only_owner_or_admin_can_read_ticket()
    {
        var ticket = CreateTicket();

        Assert.True(SupportTicketPolicy.CanAccess(ticket, "ticket-owner"));
        Assert.True(SupportTicketPolicy.CanAccess(ticket, "support-admin", isAdmin: true));
        Assert.False(SupportTicketPolicy.CanAccess(ticket, "different-user"));
        Assert.False(SupportTicketPolicy.CanAccess(ticket, null));
    }

    [Fact]
    public void Closed_ticket_cannot_receive_reply_but_admin_can_reopen_by_replying()
    {
        var ticket = CreateTicket(SupportTicketStatus.Closed);

        Assert.False(SupportTicketPolicy.CanReply(ticket, "ticket-owner"));
        Assert.True(SupportTicketPolicy.CanReply(ticket, "support-admin", isAdmin: true));
        Assert.Equal(SupportTicketStatus.Open, SupportTicketPolicy.StatusAfterReply(ticket.Status, isAdmin: false));
        Assert.Equal(SupportTicketStatus.WaitingForCustomer, SupportTicketPolicy.StatusAfterReply(ticket.Status, isAdmin: true));
    }
}
