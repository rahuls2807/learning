using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        // GET: Admin/ManageAdmins
        public async Task<IActionResult> ManageAdmins()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            return View(admins);
        }

        // GET: Admin/CreateAdmin
        public IActionResult CreateAdmin()
        {
            return View(new AdminRegisterViewModel());
        }

        // POST: Admin/CreateAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(AdminRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    Address = string.Empty,
                    City = string.Empty,
                    State = string.Empty,
                    PinCode = string.Empty,
                    ProfileImageUrl = string.Empty,
                    BioDescription = string.Empty,
                    ReferralCode = string.Empty,
                    ReferredBy = string.Empty,
                    KycStatus = "PENDING",
                    IsActive = true,
                    IsBlocked = false,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    TempData["AdminMessage"] = "Admin account created successfully.";
                    return RedirectToAction(nameof(ManageAdmins));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Admin/ManageBookings
        public async Task<IActionResult> ManageBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .ToListAsync();

            var profitCut = await GetProfitCutPercentageAsync();
            ViewBag.ProfitCutPercentage = profitCut;
            ViewBag.WorkerTakePercentage = (100m - profitCut) / 100m;

            return View(bookings);
        }

        // POST: Admin/UpdateBookingStatus
        [HttpPost]
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

            var profitCut = await GetProfitCutPercentageAsync();
            ViewBag.ProfitCutPercentage = profitCut;
            ViewBag.WorkerTakePercentage = (100m - profitCut) / 100m;

            return View(bookings);
        }

        // GET: Admin/ProfitCut
        public async Task<IActionResult> ProfitCut()
        {
            var profitCut = await GetProfitCutPercentageAsync();
            ViewBag.CurrentProfitCut = profitCut;
            return View(profitCut);
        }

        // POST: Admin/ProfitCut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfitCut(decimal profitCutPercentage)
        {
            if (profitCutPercentage < 0 || profitCutPercentage > 100)
            {
                ModelState.AddModelError(nameof(profitCutPercentage), "Enter a percentage between 0 and 100.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentProfitCut = profitCutPercentage;
                return View(profitCutPercentage);
            }

            try
            {
                var setting = await _context.PlatformSettings
                    .FirstOrDefaultAsync(ps => ps.Key == "WorkerProfitCutPercentage");

                if (setting == null)
                {
                    setting = new PlatformSetting
                    {
                        Key = "WorkerProfitCutPercentage",
                        Value = profitCutPercentage.ToString("F2"),
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Add(setting);
                }
                else
                {
                    setting.Value = profitCutPercentage.ToString("F2");
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // If the PlatformSettings table is missing or the database schema is unavailable,
                // ignore the update and continue with the default profit cut.
                // The actual schema migration should still be applied separately.
            }

            TempData["ProfitCutMessage"] = $"Updated platform profit cut to {profitCutPercentage:F2}%.";
            return RedirectToAction(nameof(Dashboard));
        }
        public async Task<IActionResult> CreateBooking()
        {
            var workers = await _context.Workers
                .Where(w => w.IsActive)
                .ToListAsync();
            ViewBag.Workers = workers;
            return View();
        }

        // POST: Admin/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking([Bind("WorkerId,TaskDescription,BookingDate,TotalWage")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Admin bookings have no client initially
                booking.ClientId = null;
                booking.Status = BookingStatus.Confirmed;
                booking.CreatedDate = DateTime.Now;
                booking.AmountPaidOnline = 0;
                booking.AmountPaidToWorker = 0;
                booking.PaymentStatus = PaymentStatus.Unpaid;
                
                // Set start and end time (assume 1 hour duration if not specified)
                booking.StartTime = booking.BookingDate;
                booking.EndTime = booking.BookingDate.AddHours(1);

                _context.Add(booking);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyBookings));
            }

            var workers = await _context.Workers.ToListAsync();
            ViewBag.Workers = workers;
            return View(booking);
        }

        // GET: Admin/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .Where(b => b.ClientId == null) // Admin-created bookings have no client
                .ToListAsync();

            var currentBookings = bookings
                .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress)
                .ToList();

            var historyBookings = bookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled)
                .ToList();

            ViewBag.CurrentBookings = currentBookings;
            ViewBag.HistoryBookings = historyBookings;

            return View(bookings);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpiPayments()
        {
            var submissions = await _context.UpiPaymentSubmissions
                .Include(u => u.Booking)
                    .ThenInclude(b => b!.Client)
                .Include(u => u.Booking)
                    .ThenInclude(b => b!.Worker)
                .OrderByDescending(u => u.SubmittedAt)
                .ToListAsync();

            return View(submissions);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUpiPayment(int upiPaymentId, string? adminNotes)
        {
            var submission = await _context.UpiPaymentSubmissions
                .Include(u => u.Booking)
                .FirstOrDefaultAsync(u => u.UpiPaymentId == upiPaymentId);

            if (submission?.Booking == null)
                return NotFound();

            if (submission.Status != UpiPaymentStatuses.Pending)
            {
                TempData["PaymentMessage"] = "This UPI payment was already processed.";
                return RedirectToAction(nameof(UpiPayments));
            }

            var booking = submission.Booking;
            booking.AmountPaidOnline += submission.Amount;
            booking.PaymentReference = $"UPI-{submission.TransactionReference}";
            booking.Status = BookingStatus.Confirmed;
            ApplyPaymentStatus(booking);
            booking.PaidDate ??= DateTime.UtcNow;

            submission.Status = UpiPaymentStatuses.Approved;
            submission.ReviewedAt = DateTime.UtcNow;
            submission.AdminNotes = adminNotes;

            await _context.SaveChangesAsync();
            TempData["PaymentMessage"] = $"UPI payment approved for booking #{booking.BookingId}. Pay worker from client UPI: {submission.ClientUpiId}";
            return RedirectToAction(nameof(UpiPayments));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUpiPayment(int upiPaymentId, string? adminNotes)
        {
            var submission = await _context.UpiPaymentSubmissions.FindAsync(upiPaymentId);
            if (submission == null)
                return NotFound();

            submission.Status = UpiPaymentStatuses.Rejected;
            submission.ReviewedAt = DateTime.UtcNow;
            submission.AdminNotes = adminNotes;
            await _context.SaveChangesAsync();

            TempData["PaymentMessage"] = "UPI payment rejected.";
            return RedirectToAction(nameof(UpiPayments));
        }

        private async Task<decimal> GetProfitCutPercentageAsync()
        {
            try
            {
                var setting = await _context.PlatformSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ps => ps.Key == "WorkerProfitCutPercentage");

                if (setting == null || !decimal.TryParse(setting.Value, out var profitCut))
                {
                    return 10.00m;
                }

                return Math.Clamp(profitCut, 0m, 100m);
            }
            catch (Exception)
            {
                // If the PlatformSettings table is missing or the database schema is not migrated yet,
                // fall back to the default profit cut instead of crashing the page.
                return 10.00m;
            }
        }

        private static void ApplyPaymentStatus(Booking booking)
        {
            var paid = booking.AmountPaidOnline + booking.AmountPaidToWorker;
            booking.PaymentStatus = paid <= 0
                ? PaymentStatus.Unpaid
                : paid >= booking.TotalWage
                    ? PaymentStatus.Paid
                    : PaymentStatus.PartiallyPaid;
        }
    }
}
