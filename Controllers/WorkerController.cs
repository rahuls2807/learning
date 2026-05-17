using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Worker,Admin")]
    public class WorkerController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public WorkerController(
            WorkerBookingContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Worker
        public async Task<IActionResult> Index(string? search, string? skill, int page = 1, int pageSize = 25)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);
            var userId = _userManager.GetUserId(User);

            var query = _context.Workers.AsNoTracking();
            query = User.IsInRole("Admin")
                ? query
                : query.Where(w => w.UserId == userId);

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
                    (w.Email != null && EF.Functions.Like(w.Email, term)) ||
                    (w.PhoneNumber != null && EF.Functions.Like(w.PhoneNumber, term)) ||
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
                    g => g.OrderByDescending(r => r.EffectiveDate).First().RatePerHour * 0.90m);

            foreach (var worker in workers)
            {
                worker.DisplayRate = rates.GetValueOrDefault(worker.WorkerId);
            }

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

        // GET: Worker/Create
        [AllowAnonymous]
        public IActionResult Create()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Client"))
            {
                return Forbid();
            }

            return View(new WorkerRegisterViewModel());
        }


        // POST: Worker/Create
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkerRegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Client"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {

                var existingWorker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.PhoneNumber == model.PhoneNumber);

                if (existingWorker != null)
                {
                    ModelState.AddModelError("PhoneNumber", "A worker with this phone number already exists.");
                }
                else
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

                    await _userManager.AddToRoleAsync(user, "Worker");

                    var worker = new Worker
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Skill = model.Skill,
                        UserId = user.Id,
                        IsActive = true
                    };

                    _context.Add(worker);
                    await _context.SaveChangesAsync();
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction(nameof(Index));
                }
            }
            return View(model);
        }

        // GET: Worker/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var worker = await _context.Workers.FindAsync(id);
            if (worker == null)
                return NotFound();
            if (!CanAccessWorker(worker)) return Forbid();

            return View(worker);
        }

        // POST: Worker/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WorkerId,FirstName,LastName,Email,PhoneNumber,Skill,IsActive")] Worker worker)
        {
            if (id != worker.WorkerId)
                return NotFound();

            var existing = await _context.Workers.AsNoTracking().FirstOrDefaultAsync(w => w.WorkerId == id);
            if (existing == null) return NotFound();
            if (!CanAccessWorker(existing)) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    worker.UserId = existing.UserId;
                    _context.Update(worker);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkerExists(worker.WorkerId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(worker);
        }

        // GET: Worker/ManageAvailability/5
        public async Task<IActionResult> ManageAvailability(int? id)
        {
            if (id == null)
                return NotFound();

            var worker = await _context.Workers
                .Include(w => w.Availabilities)
                .FirstOrDefaultAsync(w => w.WorkerId == id);

            if (worker == null)
                return NotFound();
            if (!CanAccessWorker(worker)) return Forbid();

            return View(worker);
        }

        // POST: Worker/AddAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(int workerId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null) return NotFound();
            if (!CanAccessWorker(worker)) return Forbid();

            var availability = new WorkerAvailability
            {
                WorkerId = workerId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = true
            };

            _context.Add(availability);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageAvailability", new { id = workerId });
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
            if (worker == null) return Forbid();

            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Where(b => b.WorkerId == worker.WorkerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        private bool WorkerExists(int id)
        {
            return _context.Workers.Any(e => e.WorkerId == id);
        }

        private bool CanAccessWorker(Worker worker)
        {
            return User.IsInRole("Admin") || worker.UserId == _userManager.GetUserId(User);
        }
    }
}
