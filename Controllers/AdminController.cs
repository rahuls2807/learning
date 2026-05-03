using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly WorkerBookingContext _context;

        public AdminController(WorkerBookingContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalWorkers = await _context.Workers.CountAsync();
            var totalClients = await _context.Clients.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var totalEarnings = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .SumAsync(b => b.TotalWage);

            ViewBag.TotalWorkers = totalWorkers;
            ViewBag.TotalClients = totalClients;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalEarnings = totalEarnings;

            return View();
        }

        // GET: Admin/ManageRates
        public async Task<IActionResult> ManageRates()
        {
            var rates = await _context.HourlyRates
                .Include(r => r.Worker)
                .ToListAsync();
            return View(rates);
        }

        // GET: Admin/SetRate
        public async Task<IActionResult> SetRate()
        {
            var workers = await _context.Workers
                .Where(w => w.IsActive)
                .ToListAsync();
            ViewBag.Workers = workers;
            return View();
        }

        // POST: Admin/SetRate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRate([Bind("WorkerId,Skill,RatePerHour")] HourlyRate hourlyRate)
        {
            if (hourlyRate.RatePerHour <= 0)
            {
                hourlyRate.RatePerHour = 10.00m;
            }

            if (ModelState.IsValid)
            {
                // Deactivate previous rate for this worker
                var previousRate = await _context.HourlyRates
                    .Where(hr => hr.WorkerId == hourlyRate.WorkerId && hr.IsActive)
                    .FirstOrDefaultAsync();

                if (previousRate != null)
                {
                    previousRate.IsActive = false;
                }

                hourlyRate.EffectiveDate = DateTime.Now;
                hourlyRate.IsActive = true;

                _context.Add(hourlyRate);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ManageRates));
            }

            var workers = await _context.Workers.ToListAsync();
            ViewBag.Workers = workers;
            return View(hourlyRate);
        }

        // GET: Admin/ManageBookings
        public async Task<IActionResult> ManageBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .ToListAsync();
            return View(bookings);
        }

        // POST: Admin/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return NotFound();

            booking.Status = status;
            _context.Update(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageBookings));
        }

        // GET: Admin/GenerateReport
        public async Task<IActionResult> GenerateReport(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
                startDate = DateTime.Now.AddMonths(-1);
            if (endDate == null)
                endDate = DateTime.Now;

            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .Where(b => b.CreatedDate >= startDate && b.CreatedDate <= endDate)
                .ToListAsync();

            var completedBookings = bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .ToList();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.CompletedBookings = completedBookings.Count;
            ViewBag.TotalWages = completedBookings.Sum(b => b.TotalWage);
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(bookings);
        }
    }
}
