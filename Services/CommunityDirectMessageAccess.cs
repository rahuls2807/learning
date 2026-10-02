using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services;

public static class CommunityDirectMessageAccess
{
    public static async Task<bool> CanMessageAsync(
        WorkerBookingContext context,
        UserManager<ApplicationUser> userManager,
        string senderUserId,
        string recipientUserId)
    {
        var sender = await userManager.FindByIdAsync(senderUserId);
        var recipient = await userManager.FindByIdAsync(recipientUserId);
        if (sender == null || recipient == null)
            return false;

        var senderRoles = await userManager.GetRolesAsync(sender);
        var recipientRoles = await userManager.GetRolesAsync(recipient);
        var senderRole = senderRoles.FirstOrDefault(role => role is "Client" or "Worker");
        var recipientRole = recipientRoles.FirstOrDefault(role => role is "Client" or "Worker");
        var hasBooking = false;

        if (senderRole != recipientRole)
        {
            hasBooking = await context.Bookings.AnyAsync(booking =>
                booking.Status != BookingStatus.Cancelled
                && ((booking.Client!.UserId == senderUserId && booking.Worker!.UserId == recipientUserId)
                    || (booking.Client!.UserId == recipientUserId && booking.Worker!.UserId == senderUserId)));
        }

        return CommunityDirectMessagePolicy.CanStart(senderRole, recipientRole, hasBooking);
    }
}
