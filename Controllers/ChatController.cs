using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Client,Worker")]
    public class ChatController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.Client!.UserId == userId || b.Worker!.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .Take(100)
                .Select(b => new ChatConversationViewModel
                {
                    BookingId = b.BookingId,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    TaskDescription = b.TaskDescription ?? string.Empty,
                    OtherParticipantName = b.Client!.UserId == userId
                        ? b.Worker!.FirstName + " " + b.Worker.LastName
                        : b.Client.FirstName + " " + b.Client.LastName
                })
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> Booking(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Client)
                .Include(b => b.Worker)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound();
            if (!BookingParticipantPolicy.CanAccess(booking, userId))
                return Forbid();

            var otherName = booking.Client?.UserId == userId
                ? $"{booking.Worker?.FirstName} {booking.Worker?.LastName}".Trim()
                : $"{booking.Client?.FirstName} {booking.Client?.LastName}".Trim();

            return View(new BookingChatViewModel
            {
                BookingId = booking.BookingId,
                CurrentUserId = userId ?? string.Empty,
                OtherParticipantName = string.IsNullOrWhiteSpace(otherName) ? "Booking participant" : otherName,
                TaskDescription = booking.TaskDescription ?? string.Empty,
                StartTime = booking.StartTime
            });
        }
    }
}