using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;

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
        public IActionResult Register()
        {
            return View(new ClientRegisterViewModel());
        }

        // POST: Client/Register
        [HttpPost]
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
        public async Task<IActionResult> BookWorker(string? search, string? skill, int page = 1, int pageSize = 25)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);

            var query = _context.Workers.AsNoTracking().Where(w => w.IsActive);

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
                .OrderBy(w => w.FirstName)
                .ThenBy(w => w.LastName)
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

            var clients = await _context.Clients.ToListAsync();
            ViewBag.ClientList = clients;
            ViewBag.Worker = worker;
            ViewBag.CurrentClientId = await GetCurrentClientId();

            return View();
        }

        // POST: Client/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking([Bind("WorkerId,ClientId,BookingDate,StartTime,EndTime,TaskDescription")] Booking booking)
        {
            // Custom validation
            if (booking.ClientId <= 0)
            {
                ModelState.AddModelError("ClientId", "Please select a valid client.");
            }
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
                        booking.CreatedDate = DateTime.Now;

                        _context.Add(booking);
                        await _context.SaveChangesAsync();

                        return RedirectToAction("MyBookings", new { clientId = booking.ClientId });
                    }
                }
            }

            var clients = await _context.Clients.ToListAsync();
            ViewBag.ClientList = clients;
            ViewBag.Worker = await _context.Workers.FindAsync(booking.WorkerId);
            ViewBag.CurrentClientId = await GetCurrentClientId();
            return View(booking);
        }

        // GET: Client/MyBookings/5
        public async Task<IActionResult> MyBookings(int? clientId)
        {
            clientId ??= await GetCurrentClientId();
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
