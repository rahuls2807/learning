using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Controllers
{
    public class WorkerController : Controller
    {
        private readonly WorkerBookingContext _context;

        public WorkerController(WorkerBookingContext context)
        {
            _context = context;
        }

        // GET: Worker
        public async Task<IActionResult> Index()
        {
            var workers = await _context.Workers.ToListAsync();
            return View(workers);
        }

        // GET: Worker/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Worker/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,Skill")] Worker worker)
        {
            if (ModelState.IsValid)
            {
                var existingWorker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Email == worker.Email);

                if (existingWorker != null)
                {
                    ModelState.AddModelError("Email", "A worker with this email already exists.");
                }
                else
                {
                    worker.IsActive = true; // Ensure new workers are active
                    _context.Add(worker);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(worker);
        }

        // GET: Worker/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var worker = await _context.Workers.FindAsync(id);
            if (worker == null)
                return NotFound();

            return View(worker);
        }

        // POST: Worker/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WorkerId,FirstName,LastName,Email,PhoneNumber,Skill,IsActive")] Worker worker)
        {
            if (id != worker.WorkerId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
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

            return View(worker);
        }

        // POST: Worker/AddAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(int workerId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
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

        private bool WorkerExists(int id)
        {
            return _context.Workers.Any(e => e.WorkerId == id);
        }
    }
}
