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
        public async Task<IActionResult> BookWorker()
        {
            var workers = await _context.Workers
                .Where(w => w.IsActive)
                .ToListAsync();
            return View(workers);
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
                .ToListAsync();

            return View(bookings);
        }

        private async Task<Client?> GetCurrentClient()
        {
            var userId = _userManager.GetUserId(User);
            return await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}
