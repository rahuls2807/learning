using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private const int PageSize = 25;
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1)
                return BadRequest();

            var userId = _userManager.GetUserId(User);
            var query = _context.SupportTickets.AsNoTracking();
            if (!User.IsInRole("Admin"))
                query = query.Where(t => t.CreatedByUserId == userId);

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            page = Math.Min(page, totalPages);
            var tickets = await query
                .Include(t => t.Booking)
                .Include(t => t.CreatedByUser)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? bookingId)
        {
            var model = new CreateSupportTicketViewModel { BookingId = bookingId };
            await PopulateBookingsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("support-write")]
        public async Task<IActionResult> Create(CreateSupportTicketViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!Enum.IsDefined(model.Category))
                ModelState.AddModelError(nameof(model.Category), "Select a valid ticket category.");

            if (model.BookingId.HasValue && !await CanAccessBookingAsync(model.BookingId.Value, userId))
                ModelState.AddModelError(nameof(model.BookingId), "Select one of your own bookings.");

            if (!ModelState.IsValid)
            {
                await PopulateBookingsAsync(model);
                return View(model);
            }

            var now = DateTime.UtcNow;
            var ticket = new SupportTicket
            {
                Subject = model.Subject.Trim(),
                Category = model.Category,
                BookingId = model.BookingId,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Replies =
                {
                    new SupportTicketReply
                    {
                        UserId = userId,
                        Message = model.Message.Trim(),
                        CreatedAtUtc = now
                    }
                }
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();
            TempData["SupportMessage"] = $"Support ticket #{ticket.Id} was created.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await LoadTicketAsync(id);
            if (ticket == null)
                return NotFound();
            if (!SupportTicketPolicy.CanAccess(ticket, _userManager.GetUserId(User), User.IsInRole("Admin")))
                return Forbid();

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("support-write")]
        public async Task<IActionResult> Reply(ReplySupportTicketViewModel model)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == model.TicketId);
            if (ticket == null)
                return NotFound();
            if (!SupportTicketPolicy.CanAccess(ticket, _userManager.GetUserId(User), User.IsInRole("Admin")))
                return Forbid();
            if (!SupportTicketPolicy.CanReply(ticket, _userManager.GetUserId(User), User.IsInRole("Admin")))
                return Conflict("This ticket is closed and cannot receive further replies.");

            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Details), new { id = model.TicketId });

            var now = DateTime.UtcNow;
            var userId = _userManager.GetUserId(User)!;
            _context.SupportTicketReplies.Add(new SupportTicketReply
            {
                SupportTicketId = ticket.Id,
                UserId = userId,
                Message = model.Message.Trim(),
                CreatedAtUtc = now
            });
            ticket.UpdatedAtUtc = now;

            ticket.Status = SupportTicketPolicy.StatusAfterReply(ticket.Status, User.IsInRole("Admin"));

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("support-write")]
        public async Task<IActionResult> UpdateStatus(UpdateSupportTicketViewModel model)
        {
            if (!Enum.IsDefined(model.Status))
                return BadRequest();

            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == model.TicketId);
            if (ticket == null)
                return NotFound();

            ticket.Status = model.Status;
            ticket.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SupportMessage"] = "Ticket status updated.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        private Task<SupportTicket?> LoadTicketAsync(int id)
        {
            return _context.SupportTickets
                .AsNoTracking()
                .Include(t => t.Booking)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Replies.OrderBy(r => r.CreatedAtUtc))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        private async Task<bool> CanAccessBookingAsync(int bookingId, string userId)
        {
            if (User.IsInRole("Admin"))
                return await _context.Bookings.AnyAsync(b => b.BookingId == bookingId);

            return await _context.Bookings.AnyAsync(b =>
                b.BookingId == bookingId &&
                (b.Client!.UserId == userId || b.Worker!.UserId == userId));
        }

        private async Task PopulateBookingsAsync(CreateSupportTicketViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var bookings = _context.Bookings.AsNoTracking();
            if (!User.IsInRole("Admin"))
            {
                bookings = bookings.Where(b => b.Client!.UserId == userId || b.Worker!.UserId == userId);
            }

            model.Bookings = await bookings
                .OrderByDescending(b => b.BookingDate)
                .Take(100)
                .Select(b => new SelectListItem
                {
                    Value = b.BookingId.ToString(),
                    Text = $"#{b.BookingId} · {b.BookingDate:dd MMM yyyy} · {b.Status}"
                })
                .ToListAsync();
        }
    }
}