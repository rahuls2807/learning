using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Client")]
    public class PaymentController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Pay(int bookingId)
        {
            var booking = await GetClientBooking(bookingId);
            if (booking == null) return NotFound();

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                return RedirectToAction("MyBookings", "Client");
            }

            return View(ToPaymentViewModel(booking));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(PaymentViewModel model)
        {
            var booking = await GetClientBooking(model.BookingId);
            if (booking == null) return NotFound();

            model.Amount = booking.TotalWage;
            model.WorkerName = $"{booking.Worker?.FirstName} {booking.Worker?.LastName}".Trim();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            booking.PaymentStatus = PaymentStatus.Paid;
            booking.PaymentReference = $"PAY-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
            booking.PaidDate = DateTime.Now;
            booking.Status = BookingStatus.Confirmed;

            await _context.SaveChangesAsync();
            TempData["PaymentMessage"] = "Payment completed securely. Card details were not stored.";

            return RedirectToAction("MyBookings", "Client");
        }

        private async Task<Booking?> GetClientBooking(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null) return null;

            return await _context.Bookings
                .Include(b => b.Worker)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.ClientId == client.ClientId);
        }

        private static PaymentViewModel ToPaymentViewModel(Booking booking)
        {
            return new PaymentViewModel
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalWage,
                WorkerName = $"{booking.Worker?.FirstName} {booking.Worker?.LastName}".Trim()
            };
        }
    }
}
