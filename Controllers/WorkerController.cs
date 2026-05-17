using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Worker,Admin,Client")]
    public class WorkerController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public WorkerController(
            WorkerBookingContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }

        // GET: Worker
        public async Task<IActionResult> Index(string? search, string? skill, int page = 1, int pageSize = 25)
        {
            if (User.IsInRole("Client"))
            {
                return RedirectToAction("BookWorker", "Client", new { search, skill, page, pageSize });
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);
            var userId = _userManager.GetUserId(User);

            var query = _context.Workers.AsNoTracking();
            query = User.IsInRole("Admin")
                ? query
                : User.IsInRole("Worker")
                    ? query.Where(w => w.UserId == userId)
                    : query.Where(w => w.IsActive);

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
                    Email = w.Email,
                    PhoneNumber = w.PhoneNumber,
                    Skill = w.Skill,
                    IsActive = w.IsActive,
                    ProfileImagePath = w.ProfileImagePath,
                    AverageRating = w.Reviews.Any() ? w.Reviews.Average(r => r.Rating) : null,
                    ReviewCount = w.Reviews.Count,
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
                        ProfileImagePath = await SaveWorkerFile(model.ProfileImage, "images", [".jpg", ".jpeg", ".png", ".webp"]),
                        ResumePath = await SaveWorkerFile(model.Resume, "resumes", [".pdf", ".doc", ".docx"]),
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

            return View(new WorkerEditViewModel
            {
                WorkerId = worker.WorkerId,
                FirstName = worker.FirstName ?? string.Empty,
                LastName = worker.LastName ?? string.Empty,
                Email = worker.Email ?? string.Empty,
                PhoneNumber = worker.PhoneNumber ?? string.Empty,
                Skill = worker.Skill ?? string.Empty,
                IsActive = worker.IsActive,
                CurrentProfileImagePath = worker.ProfileImagePath,
                CurrentResumePath = worker.ResumePath
            });
        }

        // POST: Worker/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkerEditViewModel model)
        {
            if (id != model.WorkerId)
                return NotFound();

            var existing = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerId == id);
            if (existing == null) return NotFound();
            if (!CanAccessWorker(existing)) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    existing.FirstName = model.FirstName;
                    existing.LastName = model.LastName;
                    existing.Email = model.Email;
                    existing.PhoneNumber = model.PhoneNumber;
                    existing.Skill = model.Skill;
                    existing.IsActive = model.IsActive;
                    existing.ProfileImagePath = await SaveWorkerFile(model.ProfileImage, "images", [".jpg", ".jpeg", ".png", ".webp"])
                        ?? existing.ProfileImagePath;
                    existing.ResumePath = await SaveWorkerFile(model.Resume, "resumes", [".pdf", ".doc", ".docx"])
                        ?? existing.ResumePath;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkerExists(model.WorkerId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            model.CurrentProfileImagePath = existing.ProfileImagePath;
            model.CurrentResumePath = existing.ResumePath;
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var worker = await _context.Workers
                .AsNoTracking()
                .Include(w => w.Reviews)
                .FirstOrDefaultAsync(w => w.WorkerId == id);

            if (worker == null) return NotFound();

            var canSeeContact = User.IsInRole("Admin")
                || (User.IsInRole("Worker") && worker.UserId == _userManager.GetUserId(User))
                || await HasClientBookedWorker(id);

            var canReview = User.IsInRole("Admin") || await HasClientBookedWorker(id);
            var bookingIdForReview = await GetClientBookingIdForWorker(id);

            var reviews = worker.Reviews
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            return View(new WorkerProfileViewModel
            {
                Worker = worker,
                CanSeeContact = canSeeContact,
                CanReview = canReview,
                BookingIdForReview = bookingIdForReview,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : null,
                ReviewCount = reviews.Count,
                Reviews = reviews
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(WorkerReviewInputViewModel model)
        {
            var worker = await _context.Workers.FindAsync(model.WorkerId);
            if (worker == null) return NotFound();

            if (!User.IsInRole("Admin") && !await HasClientBookedWorker(model.WorkerId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Details), new { id = model.WorkerId });
            }

            var client = await GetCurrentClient();
            var reviewerName = User.IsInRole("Admin")
                ? "Admin"
                : $"{client?.FirstName} {client?.LastName}".Trim();

            _context.WorkerReviews.Add(new WorkerReview
            {
                WorkerId = model.WorkerId,
                ClientId = client?.ClientId,
                BookingId = User.IsInRole("Admin") ? null : model.BookingId,
                Rating = model.Rating,
                Comment = model.Comment,
                ReviewerName = string.IsNullOrWhiteSpace(reviewerName) ? "Client" : reviewerName,
                IsAdminReview = User.IsInRole("Admin")
            });

            await _context.SaveChangesAsync();
            TempData["ReviewMessage"] = "Review saved.";
            return RedirectToAction(nameof(Details), new { id = model.WorkerId });
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

        private async Task<string?> SaveWorkerFile(IFormFile? file, string folder, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0) return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) return null;

            var relativeFolder = Path.Combine("uploads", "workers", folder);
            var absoluteFolder = Path.Combine(_environment.WebRootPath, relativeFolder);
            Directory.CreateDirectory(absoluteFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteFolder, fileName);

            await using var stream = System.IO.File.Create(absolutePath);
            await file.CopyToAsync(stream);

            return "/" + Path.Combine(relativeFolder, fileName).Replace("\\", "/");
        }

        private async Task<Client?> GetCurrentClient()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId || c.Email == User.Identity!.Name);
        }

        private async Task<bool> HasClientBookedWorker(int workerId)
        {
            var client = await GetCurrentClient();
            if (client == null) return false;

            return await _context.Bookings.AnyAsync(b => b.ClientId == client.ClientId && b.WorkerId == workerId);
        }

        private async Task<int?> GetClientBookingIdForWorker(int workerId)
        {
            var client = await GetCurrentClient();
            if (client == null) return null;

            return await _context.Bookings
                .Where(b => b.ClientId == client.ClientId && b.WorkerId == workerId)
                .OrderByDescending(b => b.BookingDate)
                .Select(b => (int?)b.BookingId)
                .FirstOrDefaultAsync();
        }
    }
}
