using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers
{
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
            var clients = await _context.Clients.ToListAsync();
            return View(clients);
        }

        // GET: Client/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new ClientRegisterViewModel());
        }

        // POST: Client/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-registration")]
        public async Task<IActionResult> Register(ClientRegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
                return Forbid();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    Address = model.Address ?? string.Empty  // Set Address to prevent NULL errors
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

                var roleResult = await _userManager.AddToRoleAsync(user, "Client");
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    foreach (var error in roleResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }

                var client = new Client
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = IndianPhoneNumber.ToE164(model.PhoneNumber),
                    Address = model.Address,
                    UserId = user.Id
                };

                _context.Add(client);
                await _context.SaveChangesAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(BookWorker));
            }
            return View(model);
        }

        // GET: Client/BookWorker
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> BookWorker(string? search, string? skill, string? sort = "recommended", int page = 1, int pageSize = 25)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);
            sort = sort is "rating" or "completed" or "rate-low" or "rate-high" ? sort : "recommended";

            var query = _context.Workers.AsNoTracking().Where(w => w.IsActive);

            if (!string.IsNullOrWhiteSpace(skill))
            {
                query = query.Where(w => w.Skill != null && w.Skill == skill);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                query = query.Where(w =>
                    (w.FirstName != null && EF.Functions.Like(w.FirstName, term)) ||
                    (w.LastName != null && EF.Functions.Like(w.LastName, term)) ||
                    (w.Skill != null && EF.Functions.Like(w.Skill, term)));
            }

            query = sort switch
            {
                "rating" => query.OrderByDescending(w => w.Reviews.Any() ? w.Reviews.Average(r => r.Rating) : 0)
                    .ThenByDescending(w => w.Reviews.Count),
                "completed" => query.OrderByDescending(w => w.Bookings.Count(b => b.Status == BookingStatus.Completed)),
                "rate-low" => query.OrderBy(w => _context.HourlyRates.Where(r => r.WorkerId == w.WorkerId && r.IsActive)
                    .OrderByDescending(r => r.EffectiveDate).Select(r => (decimal?)r.RatePerHour).FirstOrDefault()),
                "rate-high" => query.OrderByDescending(w => _context.HourlyRates.Where(r => r.WorkerId == w.WorkerId && r.IsActive)
                    .OrderByDescending(r => r.EffectiveDate).Select(r => (decimal?)r.RatePerHour).FirstOrDefault()),
                _ => query.OrderByDescending(w => w.Reviews.Any() ? w.Reviews.Average(r => r.Rating) : 0)
                    .ThenByDescending(w => w.Reviews.Count)
                    .ThenByDescending(w => w.Bookings.Count(b => b.Status == BookingStatus.Completed))
            };

            var totalItems = await query.CountAsync();
            var workers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WorkerSearchItemViewModel
                {
                    WorkerId = w.WorkerId,
                    Name = ((w.FirstName ?? "") + " " + (w.LastName ?? "")).Trim(),
                    Skill = w.Skill,
                    IsActive = w.IsActive,
                    ProfileImagePath = w.ProfileImagePath,
                    AverageRating = w.Reviews.Any() ? w.Reviews.Average(r => r.Rating) : null,
                    ReviewCount = w.Reviews.Count,
                    CompletedJobs = w.Bookings.Count(b => b.Status == BookingStatus.Completed),
                    CreatedDate = w.CreatedDate
                })
                .ToListAsync();

            var workerIds = workers.Select(w => w.WorkerId).ToList();
            var activeRates = await _context.HourlyRates
                .AsNoTracking()
                .Where(r => workerIds.Contains(r.WorkerId) && r.IsActive)
                .ToListAsync();
            var rates = activeRates
                .GroupBy(r => r.WorkerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.EffectiveDate).First().RatePerHour);

            foreach (var worker in workers)
            {
                worker.DisplayRate = rates.GetValueOrDefault(worker.WorkerId);
            }

            ViewBag.Skills = await _context.Workers
                .Where(w => w.IsActive && w.Skill != null)
                .Select(w => w.Skill)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return View(new PagedResult<WorkerSearchItemViewModel>
            {
                Items = workers,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                Search = search,
                Skill = skill,
                Sort = sort
            });
        }

        // GET: Client/CreateBooking/5
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CreateBooking(int? workerId)
        {
            if (workerId == null)
                return NotFound();

            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null || !worker.IsActive)
                return NotFound();

            var hourlyRate = await _context.HourlyRates
                .Where(hr => hr.WorkerId == worker.WorkerId && hr.IsActive)
                .OrderByDescending(hr => hr.EffectiveDate)
                .Select(hr => hr.RatePerHour)
                .FirstOrDefaultAsync();

            ViewBag.Worker = worker;
            ViewBag.WorkerRate = hourlyRate > 0 ? (decimal?)hourlyRate : null;

            return View(new Booking { WorkerId = worker.WorkerId });
        }

        // POST: Client/CreateBooking
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking([Bind("WorkerId,BookingDate,StartTime,EndTime,TaskDescription")] Booking booking)
        {
            var clientId = await GetCurrentClientId();
            if (clientId == null)
            {
                return Forbid();
            }

            booking.ClientId = clientId;
            var worker = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkerId == booking.WorkerId && w.IsActive);
            if (worker == null)
            {
                return NotFound();
            }

            if (booking.StartTime >= booking.EndTime)
                ModelState.AddModelError("EndTime", "End time must be after start time.");

            if (booking.BookingDate.Date < DateTime.Today || booking.StartTime <= DateTime.Now)
                ModelState.AddModelError("BookingDate", "Choose a future booking date and time.");

            if (booking.BookingDate.Date != booking.StartTime.Date || booking.StartTime.Date != booking.EndTime.Date)
                ModelState.AddModelError("EndTime", "A booking must start and end on the selected date.");

            if (ModelState.IsValid)
            {
                var overlappingBooking = await _context.Bookings.AnyAsync(b =>
                    b.WorkerId == booking.WorkerId &&
                    b.Status != BookingStatus.Cancelled &&
                    b.StartTime < booking.EndTime &&
                    b.EndTime > booking.StartTime);

                if (overlappingBooking)
                {
                    ModelState.AddModelError(string.Empty, "This worker already has a booking during that time. Choose another time.");
                }
                else
                {
                    var dayAvailability = await _context.WorkerAvailabilities
                        .AsNoTracking()
                        .Where(a => a.WorkerId == booking.WorkerId && a.DayOfWeek == booking.StartTime.DayOfWeek)
                        .ToListAsync();

                    var hasMatchingAvailability = dayAvailability.Any(a =>
                        a.IsAvailable &&
                        a.StartTime <= booking.StartTime.TimeOfDay &&
                        a.EndTime >= booking.EndTime.TimeOfDay);

                    if (dayAvailability.Count > 0 && !hasMatchingAvailability)
                    {
                        ModelState.AddModelError(string.Empty, "The selected time is outside this worker's published availability.");
                    }

                    // Calculate wage
                    var hourlyRate = await _context.HourlyRates
                        .Where(hr => hr.WorkerId == booking.WorkerId && hr.IsActive)
                        .OrderByDescending(hr => hr.EffectiveDate)
                        .Select(hr => hr.RatePerHour)
                        .FirstOrDefaultAsync();

                    if (ModelState.IsValid && hourlyRate <= 0)
                    {
                        ModelState.AddModelError("", "No active hourly rate for this worker. Contact admin.");
                    }
                    else if (ModelState.IsValid)
                    {
                        var hours = (booking.EndTime - booking.StartTime).TotalHours;
                        booking.TotalWage = (decimal)hours * hourlyRate;

                        booking.Status = BookingStatus.Pending;
                        booking.CreatedDate = DateTime.Now;

                        _context.Add(booking);
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(MyBookings));
                    }
                }
            }

            ViewBag.Worker = worker;
            ViewBag.WorkerRate = await _context.HourlyRates
                .Where(hr => hr.WorkerId == worker.WorkerId && hr.IsActive)
                .OrderByDescending(hr => hr.EffectiveDate)
                .Select(hr => (decimal?)hr.RatePerHour)
                .FirstOrDefaultAsync();
            return View(booking);
        }

        // GET: Client/MyBookings/5
        [Authorize(Roles = "Client,Admin")]
        public async Task<IActionResult> MyBookings(int? clientId)
        {
            clientId = User.IsInRole("Admin") ? clientId ?? await GetCurrentClientId() : await GetCurrentClientId();
            if (clientId == null)
                return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .Where(b => b.ClientId == clientId)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(ClientBookingStatusViewModel model)
        {
            var booking = await GetCurrentClientBooking(model.BookingId);
            if (booking == null) return NotFound();

            if (!ModelState.IsValid)
            {
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
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordCashPayment(ClientCashPaymentViewModel model)
        {
            var booking = await GetCurrentClientBooking(model.BookingId);
            if (booking == null) return NotFound();

            var balance = booking.TotalWage - booking.AmountPaidOnline - booking.AmountPaidToWorker;
            if (model.AmountPaidToWorker <= 0 || model.AmountPaidToWorker > balance)
            {
                TempData["BookingMessage"] = "Cash payment must be greater than zero and no more than the remaining balance.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.AmountPaidToWorker += model.AmountPaidToWorker;
            booking.ClientStatusNote = model.ClientStatusNote;
            booking.LastClientStatusUpdate = DateTime.Now;
            booking.PaymentStatus = booking.AmountPaidOnline + booking.AmountPaidToWorker >= booking.TotalWage
                ? PaymentStatus.Paid
                : PaymentStatus.PartiallyPaid;

            if (booking.PaymentStatus == PaymentStatus.Paid && booking.PaidDate == null)
            {
                booking.PaidDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["BookingMessage"] = "Cash payment recorded.";
            return RedirectToAction(nameof(MyBookings));
        }

        // Add real-time chat feature
        public IActionResult Chat()
        {
            return View();
        }

        private async Task<int?> GetCurrentClientId()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(User.Identity?.Name))
            {
                return null;
            }

            return await _context.Clients
                .Where(c => c.UserId == userId || c.Email == User.Identity!.Name)
                .Select(c => (int?)c.ClientId)
                .FirstOrDefaultAsync();
        }

        private async Task<Booking?> GetCurrentClientBooking(int bookingId)
        {
            var clientId = await GetCurrentClientId();
            if (clientId == null) return null;

            return await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId && b.ClientId == clientId);
        }
    }
}
