using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    public class CommunityChatController : Controller
    {
        private const int InboxLimit = 50;
        private const int MessageHistoryLimit = 50;
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityChatController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
                return Challenge();

            var rows = await _context.CommunityConversations.AsNoTracking()
                .Where(conversation => conversation.UserOneId == currentUserId || conversation.UserTwoId == currentUserId)
                .OrderByDescending(conversation => conversation.LastMessageAtUtc)
                .Take(InboxLimit)
                .Select(conversation => new
                {
                    conversation.Id,
                    OtherUserId = conversation.UserOneId == currentUserId ? conversation.UserTwoId : conversation.UserOneId,
                    conversation.LastMessageAtUtc,
                    LastMessage = conversation.Messages.OrderByDescending(message => message.SentAtUtc)
                        .Select(message => message.Content).FirstOrDefault() ?? string.Empty,
                    UnreadCount = conversation.Messages.Count(message => message.SenderUserId != currentUserId && message.ReadAtUtc == null)
                })
                .ToListAsync();

            var names = await GetNamesAsync(rows.Select(row => row.OtherUserId).Distinct().ToList());
            var model = rows.Select(row =>
            {
                var name = names.GetValueOrDefault(row.OtherUserId) ?? "Community member";
                return new CommunityInboxItemViewModel
                {
                    ConversationId = row.Id,
                    OtherUserName = name,
                    OtherUserInitials = CommunityProfilePolicy.GetInitials(name),
                    LastMessage = row.LastMessage,
                    LastMessageAtUtc = row.LastMessageAtUtc,
                    UnreadCount = row.UnreadCount
                };
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> Start(string memberUserId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
                return Challenge();
            if (string.IsNullOrWhiteSpace(memberUserId) || memberUserId == currentUserId)
                return BadRequest("Choose another community member.");
            if (!await _context.Users.AnyAsync(user => user.Id == memberUserId && user.IsActive && !user.IsBlocked))
                return NotFound();

            if (!await CommunityDirectMessageAccess.CanMessageAsync(_context, _userManager, currentUserId, memberUserId))
                return Forbid();

            var (userOneId, userTwoId) = CommunityConversationPolicy.OrderPair(currentUserId, memberUserId);
            var conversation = await _context.CommunityConversations
                .FirstOrDefaultAsync(item => item.UserOneId == userOneId && item.UserTwoId == userTwoId);
            if (conversation == null)
            {
                conversation = new CommunityConversation
                {
                    UserOneId = userOneId,
                    UserTwoId = userTwoId,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastMessageAtUtc = DateTime.UtcNow
                };
                _context.CommunityConversations.Add(conversation);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
                {
                    _context.Entry(conversation).State = EntityState.Detached;
                    conversation = await _context.CommunityConversations
                        .FirstAsync(item => item.UserOneId == userOneId && item.UserTwoId == userTwoId);
                }
            }

            return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
        }

        public async Task<IActionResult> Conversation(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
                return Challenge();

            var conversation = await _context.CommunityConversations.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && (item.UserOneId == currentUserId || item.UserTwoId == currentUserId));
            if (conversation == null || !CommunityConversationPolicy.CanAccess(conversation, currentUserId))
                return NotFound();
            var otherUserId = conversation.UserOneId == currentUserId ? conversation.UserTwoId : conversation.UserOneId;
            if (!await CommunityDirectMessageAccess.CanMessageAsync(_context, _userManager, currentUserId, otherUserId))
                return Forbid();

            await _context.CommunityDirectMessages
                .Where(message => message.ConversationId == id && message.SenderUserId != currentUserId && message.ReadAtUtc == null)
                .ExecuteUpdateAsync(update => update.SetProperty(message => message.ReadAtUtc, DateTime.UtcNow));

            var names = await GetNamesAsync(new List<string> { otherUserId });
            var otherName = names.GetValueOrDefault(otherUserId) ?? "Community member";
            var messages = await _context.CommunityDirectMessages.AsNoTracking()
                .Where(message => message.ConversationId == id)
                .OrderByDescending(message => message.SentAtUtc)
                .Take(MessageHistoryLimit)
                .Select(message => new CommunityDirectMessageViewModel
                {
                    SenderUserId = message.SenderUserId,
                    Content = message.Content,
                    SentAtUtc = message.SentAtUtc
                })
                .ToListAsync();

            foreach (var message in messages)
                message.SenderName = message.SenderUserId == currentUserId ? "You" : otherName;

            return View(new CommunityDirectConversationViewModel
            {
                ConversationId = id,
                CurrentUserId = currentUserId,
                OtherUserName = otherName,
                OtherUserInitials = CommunityProfilePolicy.GetInitials(otherName),
                Messages = messages.OrderBy(message => message.SentAtUtc).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> Send(SendCommunityDirectMessageViewModel model)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
                return Challenge();
            if (!ModelState.IsValid)
            {
                TempData["CommunityChatError"] = "Messages must contain 1 to 2,000 characters.";
                return RedirectToAction(nameof(Conversation), new { id = model.ConversationId });
            }

            var conversation = await _context.CommunityConversations.FirstOrDefaultAsync(item =>
                item.Id == model.ConversationId && (item.UserOneId == currentUserId || item.UserTwoId == currentUserId));
            if (conversation == null)
                return NotFound();
            var otherUserId = conversation.UserOneId == currentUserId ? conversation.UserTwoId : conversation.UserOneId;
            if (!await CommunityDirectMessageAccess.CanMessageAsync(_context, _userManager, currentUserId, otherUserId))
                return Forbid();

            var now = DateTime.UtcNow;
            _context.CommunityDirectMessages.Add(new CommunityDirectMessage
            {
                ConversationId = conversation.Id,
                SenderUserId = currentUserId,
                Content = model.Content.Trim(),
                SentAtUtc = now
            });
            conversation.LastMessageAtUtc = now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
        }

        private async Task<Dictionary<string, string>> GetNamesAsync(List<string> userIds)
        {
            if (userIds.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users.AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new { user.Id, user.Email })
                .ToListAsync();
            var workers = await _context.Workers.AsNoTracking()
                .Where(worker => worker.UserId != null && userIds.Contains(worker.UserId))
                .Select(worker => new { worker.UserId, worker.FirstName, worker.LastName })
                .ToListAsync();
            var clients = await _context.Clients.AsNoTracking()
                .Where(client => client.UserId != null && userIds.Contains(client.UserId))
                .Select(client => new { client.UserId, client.FirstName, client.LastName })
                .ToListAsync();

            return users.ToDictionary(user => user.Id, user => CommunityProfilePolicy.GetDisplayName(
                new ApplicationUser { Email = user.Email },
                workers.FirstOrDefault(worker => worker.UserId == user.Id) is { } worker ? $"{worker.FirstName} {worker.LastName}" : null,
                clients.FirstOrDefault(client => client.UserId == user.Id) is { } client ? $"{client.FirstName} {client.LastName}" : null));
        }

    }
}
