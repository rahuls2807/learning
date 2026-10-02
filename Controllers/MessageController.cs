using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Hubs;
using WorkerBookingSystem.Models;
using System.ComponentModel.DataAnnotations;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<BookingChatHub> _chatHub;

        public MessageController(
            WorkerBookingContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<BookingChatHub> chatHub)
        {
            _context = context;
            _userManager = userManager;
            _chatHub = chatHub;
        }

        /// <summary>
        /// Get messages for a specific booking
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetBookingMessages(int bookingId, [FromQuery] int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (page is < 1 or > 100000)
                return BadRequest("Page must be between 1 and 100000.");

            var booking = await _context.Bookings.AsNoTracking()
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound("Booking not found");

            // Verify user is part of this booking
            if (!BookingParticipantPolicy.CanAccess(booking, userId, User.IsInRole("Admin")))
                return Forbid("Not authorized to view these messages");

            await _context.Messages
                .Where(m => m.BookingId == bookingId && m.ReceiverId == userId && m.ReadAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(m => m.ReadAt, DateTime.UtcNow));

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.BookingId == bookingId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * 50)
                .Take(50)
                .Select(m => new
                {
                    m.Id,
                    m.BookingId,
                    m.SenderId,
                    senderName = m.Sender.UserName,
                    m.Content,
                    m.MessageType,
                    m.SentAt,
                    m.ReadAt
                })
                .ToListAsync();

            return Ok(new
            {
                bookingId,
                messageCount = messages.Count,
                messages = messages.OrderBy(m => m.SentAt)
            });
        }

        /// <summary>
        /// Send a message in a booking
        /// </summary>
        [HttpPost("send")]
        [EnableRateLimiting("message-send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var senderId = _userManager.GetUserId(User);
            if (senderId == null)
                return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);

            if (booking == null)
                return NotFound("Booking not found");

            // Determine receiver based on sender role
            string? receiverId;
            if (booking.Worker?.UserId == senderId)
            {
                receiverId = booking.Client?.UserId;
            }
            else if (booking.Client?.UserId == senderId)
            {
                receiverId = booking.Worker?.UserId;
            }
            else
            {
                return Forbid("Not authorized to send messages in this booking");
            }

            if (string.IsNullOrWhiteSpace(receiverId))
                return Conflict("The other participant does not have an active account.");

            var now = DateTime.UtcNow;
            var message = new Message
            {
                BookingId = request.BookingId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = request.Content.Trim(),
                MessageType = "MESSAGE",
                SentAt = now,
                Attachments = string.Empty
            };

            _context.Messages.Add(message);
            _context.UserNotifications.Add(new UserNotification
            {
                UserId = receiverId,
                NotificationType = "MESSAGE_RECEIVED",
                Title = "New message",
                Message = $"You have a new message about booking {request.BookingId}",
                BookingId = request.BookingId,
                CreatedAt = now,
                ActionUrl = $"/Chat/Booking?bookingId={request.BookingId}"
            });
            await _context.SaveChangesAsync();

            await _chatHub.Clients.Group($"booking:{request.BookingId}").SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                bookingId = message.BookingId,
                senderId,
                senderName = User.Identity?.Name,
                content = message.Content,
                sentAt = now
            });

            return Ok(new { messageId = message.Id, sentAt = message.SentAt });
        }

        /// <summary>
        /// Get conversation list for current user
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = _userManager.GetUserId(User);

            var conversations = await _context.Messages
                .AsNoTracking()
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted)
                .GroupBy(m => m.BookingId)
                .Select(g => new
                {
                    bookingId = g.Key,
                    lastMessage = g.OrderByDescending(m => m.SentAt).Select(m => new
                    {
                        m.Content,
                        m.SentAt,
                        m.SenderId
                    }).First(),
                    unreadCount = g.Count(m => m.ReceiverId == userId && m.ReadAt == null),
                    messageCount = g.Count()
                })
                .OrderByDescending(c => c.lastMessage.SentAt)
                .ToListAsync();

            return Ok(conversations);
        }

        /// <summary>
        /// Delete a message
        /// </summary>
        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = _userManager.GetUserId(User);
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null)
                return NotFound("Message not found or not authorized");

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class SendMessageRequest
    {
        [Range(1, int.MaxValue)]
        public int BookingId { get; set; }

        [Required, StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }
}
