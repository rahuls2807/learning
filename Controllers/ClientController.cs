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
    public class ClientController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ClientController(
            WorkerBookingContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Client
        public async Task<IActionResult> Index()
        {
            return RedirectToAction(nameof(MyBookings));
        }

        // GET: Client/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new ClientRegisterViewModel());
        }

        // POST: Client/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(ClientRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, "Client");

                var client = new Client
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    UserId = user.Id,
                    IsActive = true
                };

                _context.Add(client);
                await _context.SaveChangesAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction(nameof(BookWorker));
            }
            return View(model);
        }

        // GET: Client/BookWorker
        public async Task<IActionResult> BookWorker(string? search, string? skill, int page = 1, int pageSize = 24)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 12, 60);

            var query = _context.Workers
                .AsNoTracking()
                .Where(w => w.IsActive);

            if (!string.IsNullOrWhiteSpace(skill))
            {
                query = query.Where(w => w.Skill != null && w.Skill == skill);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"{search.Trim()}%";
                query = query.Where(w =>
                    (w.FirstName != null && EF.Functions.Like(w.FirstName, term)) ||
                    (w.LastName != null && EF.Functions.Like(w.LastName, term)) ||
                    (w.Skill != null && EF.Functions.Like(w.Skill, term)));
            }

            var totalItems = await query.CountAsync();
            var workers = await query
                .OrderBy(w => w.Skill)
                .ThenBy(w => w.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WorkerSearchItemViewModel
                {
                    WorkerId = w.WorkerId,
                    Name = ((w.FirstName ?? "") + " " + (w.LastName ?? "")).Trim(),
                    Skill = w.Skill,
                    IsActive = w.IsActive,
                    CreatedDate = w.CreatedDate
                })
                .ToListAsync();

            var workerIds = workers.Select(w => w.WorkerId).ToList();
            var activeRates = await _context.HourlyRates
                .AsNoTracking()
                .Where(r => workerIds.Contains(r.WorkerId) && r.IsActive)
                .ToListAsync();
            var completedJobs = await _context.Bookings
                .AsNoTracking()
                .Where(b => workerIds.Contains(b.WorkerId) && b.Status == BookingStatus.Completed)
                .GroupBy(b => b.WorkerId)
                .Select(g => new { WorkerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WorkerId, x => x.Count);

            foreach (var worker in workers)
            {
                worker.DisplayRate = activeRates
                    .Where(r => r.WorkerId == worker.WorkerId)
                    .OrderByDescending(r => r.EffectiveDate)
                    .Select(r => r.RatePerHour)
                    .FirstOrDefault();
                worker.CompletedJobs = completedJobs.GetValueOrDefault(worker.WorkerId);
            }

            ViewBag.Skills = await _context.Workers
                .AsNoTracking()
                .Where(w => w.IsActive && w.Skill != null)
                .Select(w => w.Skill!)
                .Distinct()
                .OrderBy(s => s)
                .Take(100)
                .ToListAsync();

            return View(new PagedResult<WorkerSearchItemViewModel>
            {
                Items = workers,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                Search = search,
                Skill = skill
            });
        }

        // GET: Client/CreateBooking/5
        public async Task<IActionResult> CreateBooking(int? workerId)
        {
            if (workerId == null)
                return NotFound();

            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null)
                return NotFound();

            ViewBag.Worker = worker;

            return View();
        }

        // POST: Client/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking([Bind("WorkerId,BookingDate,StartTime,EndTime,TaskDescription")] Booking booking)
        {
            var client = await GetCurrentClient();
            if (client == null)
            {
                return Forbid();
            }

            booking.ClientId = client.ClientId;

            // Custom validation
            if (booking.StartTime >= booking.EndTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }
            if (ModelState.IsValid)
            {
                var duplicateBooking = await _context.Bookings
                    .AnyAsync(b => b.WorkerId == booking.WorkerId
                        && b.ClientId == booking.ClientId
                        && b.BookingDate == booking.BookingDate
                        && b.StartTime == booking.StartTime
                        && b.EndTime == booking.EndTime
                        && b.TaskDescription == booking.TaskDescription);

                if (duplicateBooking)
                {
                    ModelState.AddModelError("", "This booking already exists.");
                }
                else
                {
                    // Calculate wage
                    var hourlyRate = await _context.HourlyRates
                        .Where(hr => hr.WorkerId == booking.WorkerId && hr.IsActive)
                        .OrderByDescending(hr => hr.EffectiveDate)
                        .FirstOrDefaultAsync();

                    if (hourlyRate == null)
                    {
                        ModelState.AddModelError("", "No active hourly rate for this worker. Contact admin.");
                    }
                    else
                    {
                        var hours = (booking.EndTime - booking.StartTime).TotalHours;
                        booking.TotalWage = (decimal)hours * hourlyRate.RatePerHour;

                        booking.Status = BookingStatus.Pending;
                        booking.PaymentStatus = PaymentStatus.Unpaid;
                        booking.AmountPaidOnline = 0;
                        booking.AmountPaidToWorker = 0;
                        booking.CreatedDate = DateTime.Now;

                        _context.Add(booking);
                        await _context.SaveChangesAsync();

                        return RedirectToAction("Pay", "Payment", new { bookingId = booking.BookingId });
                    }
                }
            }

            ViewBag.Worker = await _context.Workers.FindAsync(booking.WorkerId);
            return View(booking);
        }

        // GET: Client/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var client = await GetCurrentClient();
            if (client == null) return Forbid();

            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .Where(b => b.ClientId == client.ClientId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(ClientBookingStatusViewModel model)
        {
            var booking = await GetCurrentClientBooking(model.BookingId);
            if (booking == null) return NotFound();

            if (!IsClientAllowedStatus(model.Status))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["BookingMessage"] = "Unable to update status. Please check the note length.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = model.Status;
            booking.ClientStatusNote = model.ClientStatusNote;
            booking.LastClientStatusUpdate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["BookingMessage"] = "Booking status updated.";
            return RedirectToAction(nameof(MyBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordCashPayment(ClientCashPaymentViewModel model)
        {
            var booking = await GetCurrentClientBooking(model.BookingId);
            if (booking == null) return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["BookingMessage"] = "Enter a valid cash amount.";
                return RedirectToAction(nameof(MyBookings));
            }

            var remainingBalance = booking.TotalWage - booking.AmountPaidOnline - booking.AmountPaidToWorker;
            if (model.AmountPaidToWorker > remainingBalance)
            {
                TempData["BookingMessage"] = "Cash payment cannot be more than the remaining balance.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.AmountPaidToWorker += model.AmountPaidToWorker;
            booking.ClientStatusNote = model.ClientStatusNote;
            booking.LastClientStatusUpdate = DateTime.Now;
            UpdatePaymentStatus(booking);

            await _context.SaveChangesAsync();
            TempData["BookingMessage"] = "Cash payment recorded.";
            return RedirectToAction(nameof(MyBookings));
        }

        private async Task<Client?> GetCurrentClient()
        {
            var userId = _userManager.GetUserId(User);
            return await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private async Task<Booking?> GetCurrentClientBooking(int bookingId)
        {
            var client = await GetCurrentClient();
            if (client == null) return null;

            return await _context.Bookings
                .Include(b => b.Worker)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.ClientId == client.ClientId);
        }

        private static bool IsClientAllowedStatus(BookingStatus status)
        {
            return status is BookingStatus.Confirmed
                or BookingStatus.InProgress
                or BookingStatus.Completed
                or BookingStatus.Cancelled;
        }

        private static void UpdatePaymentStatus(Booking booking)
        {
            var paid = booking.AmountPaidOnline + booking.AmountPaidToWorker;
            booking.PaymentStatus = paid <= 0
                ? PaymentStatus.Unpaid
                : paid >= booking.TotalWage
                    ? PaymentStatus.Paid
                    : PaymentStatus.PartiallyPaid;

            if (booking.PaymentStatus == PaymentStatus.Paid && booking.PaidDate == null)
            {
                booking.PaidDate = DateTime.Now;
            }
        }
    }
}
