using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;

namespace WorkerBookingSystem.Controllers
{
    public class ClientController : Controller
    {
        private readonly WorkerBookingContext _context;

        public ClientController(WorkerBookingContext context)
        {
            _context = context;
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
            return View();
        }

        // POST: Client/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("FirstName,LastName,Email,PhoneNumber,Address")] Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(BookWorker));
            }
            return View(client);
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
                    Email = w.Email,
                    PhoneNumber = w.PhoneNumber,
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
            return View(booking);
        }

        // GET: Client/MyBookings/5
        public async Task<IActionResult> MyBookings(int? clientId)
        {
            if (clientId == null)
                return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Client)
                .Where(b => b.ClientId == clientId)
                .ToListAsync();

            return View(bookings);
        }

        // Add real-time chat feature
        public IActionResult Chat()
        {
            return View();
        }
    }
}
