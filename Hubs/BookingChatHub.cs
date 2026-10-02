using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Hubs
{
    [Authorize]
    public class BookingChatHub : Hub
    {
        private const int HistoryLimit = 50;
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingChatHub(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<object> JoinBooking(int bookingId)
        {
            var userId = _userManager.GetUserId(Context.User!);
            var email = Context.User?.Identity?.Name;
            var booking = await GetBookingAsync(bookingId);
            if (booking == null || !BookingParticipantPolicy.CanAccess(booking, userId, email))
            {
                throw new HubException("You do not have access to this booking conversation.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(bookingId));

            var unread = _context.Messages.Where(m =>
                m.BookingId == bookingId && m.ReceiverId == userId && m.ReadAt == null);
            await unread.ExecuteUpdateAsync(update => update.SetProperty(m => m.ReadAt, DateTime.UtcNow));

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.BookingId == bookingId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Take(HistoryLimit)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.Content,
                    m.SentAt
                })
                .ToListAsync();

            return new
            {
                bookingId,
                messages = messages.OrderBy(m => m.SentAt)
            };
        }

        public async Task SendMessage(int bookingId, string? content)
        {
            var trimmedContent = content?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent.Length > 2000)
            {
                throw new HubException("Messages must contain 1 to 2,000 characters.");
            }

            var senderId = _userManager.GetUserId(Context.User!);
            var senderEmail = Context.User?.Identity?.Name;
            var booking = await GetBookingAsync(bookingId);
            if (booking == null || !BookingParticipantPolicy.CanAccess(booking, senderId, senderEmail))
            {
                throw new HubException("You do not have access to this booking conversation.");
            }

            var recipient = BookingParticipantPolicy.GetOtherParticipant(booking, senderId, senderEmail);
            var receiverId = recipient.UserId;
            if (string.IsNullOrWhiteSpace(receiverId) && !string.IsNullOrWhiteSpace(recipient.Email))
            {
                receiverId = (await _userManager.FindByEmailAsync(recipient.Email))?.Id;
            }
            if (string.IsNullOrWhiteSpace(receiverId))
            {
                throw new HubException("The other booking participant does not have an active account.");
            }

            var sentAt = DateTime.UtcNow;
            var message = new Message
            {
                BookingId = bookingId,
                SenderId = senderId!,
                ReceiverId = receiverId,
                Content = trimmedContent,
                MessageType = "MESSAGE",
                SentAt = sentAt,
                Attachments = string.Empty
            };

            _context.Messages.Add(message);
            _context.UserNotifications.Add(new UserNotification
            {
                UserId = receiverId,
                NotificationType = "MESSAGE_RECEIVED",
                Title = "New booking message",
                Message = $"You have a new message about booking {bookingId}.",
                BookingId = bookingId,
                CreatedAt = sentAt,
                ActionUrl = $"/Chat/Booking?bookingId={bookingId}"
            });
            await _context.SaveChangesAsync();

            var senderName = Context.User?.Identity?.Name;
            await Clients.Group(GroupName(bookingId)).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                bookingId,
                senderId,
                senderName,
                content = message.Content,
                sentAt
            });
        }

        private Task<Booking?> GetBookingAsync(int bookingId)
        {
            return _context.Bookings
                .AsNoTracking()
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        private static string GroupName(int bookingId) => $"booking:{bookingId}";
    }
}
