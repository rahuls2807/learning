using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get messages for a specific booking
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetBookingMessages(int bookingId, [FromQuery] int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound("Booking not found");

            // Verify user is part of this booking
            if (booking.Worker.UserId != userId && booking.Client?.UserId != userId)
                return Forbid("Not authorized to view these messages");

            var messages = await _context.Messages
                .Where(m => m.BookingId == bookingId && !m.IsDeleted)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * 50)
                .Take(50)
                .ToListAsync();

            // Mark messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == userId && m.ReadAt == null).ToList();
            foreach (var msg in unreadMessages)
            {
                msg.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var senderId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);

            if (booking == null)
                return NotFound("Booking not found");

            // Determine receiver based on sender role
            string receiverId;
            if (booking.Worker.UserId == senderId)
            {
                receiverId = booking.Client.UserId;
            }
            else if (booking.Client?.UserId == senderId)
            {
                receiverId = booking.Worker.UserId;
            }
            else
            {
                return Forbid("Not authorized to send messages in this booking");
            }

            var message = new Message
            {
                BookingId = request.BookingId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = request.Content,
                MessageType = request.MessageType ?? "MESSAGE",
                SentAt = DateTime.UtcNow,
                Attachments = request.Attachments
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Create notification for receiver
            var notification = new UserNotification
            {
                UserId = receiverId,
                NotificationType = "MESSAGE_RECEIVED",
                Title = "New message",
                Message = $"You have a new message about booking {request.BookingId}",
                BookingId = request.BookingId,
                CreatedAt = DateTime.UtcNow,
                ActionUrl = $"/Booking/{request.BookingId}/Messages"
            };
            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();

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
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted)
                .Include(m => m.Booking)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .GroupBy(m => m.BookingId)
                .Select(g => new
                {
                    bookingId = g.Key,
                    lastMessage = g.OrderByDescending(m => m.SentAt).First(),
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
        public int BookingId { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; }
        public string Attachments { get; set; }
    }
}
