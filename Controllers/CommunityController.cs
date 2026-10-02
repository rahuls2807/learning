using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    public class CommunityController : Controller
    {
        private const int PageSize = 20;
        private const int CommentPreviewCount = 3;
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int page = 1, string? shared = null)
        {
            if (page < 1 || page > 100000)
                return BadRequest();

            var currentUserId = _userManager.GetUserId(User);
            IQueryable<CommunityPost> query = _context.CommunityPosts.AsNoTracking().Where(p => !p.IsHidden);
            if (!string.IsNullOrWhiteSpace(shared))
            {
                var sharedPostId = DecodeShareId(shared);
                if (sharedPostId == null)
                    return NotFound();
                query = query.Where(p => p.Id == sharedPostId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            page = Math.Min(page, totalPages);
            var posts = await query
                .OrderByDescending(p => p.CreatedAtUtc)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var postIds = posts.Select(post => post.Id).ToList();
            var comments = await _context.CommunityPosts.AsNoTracking()
                .Where(post => postIds.Contains(post.Id))
                .SelectMany(post => post.Comments
                    .Where(comment => !comment.IsHidden)
                    .OrderByDescending(comment => comment.CreatedAtUtc)
                    .ThenByDescending(comment => comment.Id)
                    .Take(CommentPreviewCount), (post, comment) => new CommunityCommentFeedRow
                {
                    Id = comment.Id,
                    PostId = comment.PostId,
                    AuthorUserId = comment.AuthorUserId,
                    Content = comment.Content,
                    CreatedAtUtc = comment.CreatedAtUtc
                })
                .ToListAsync();

            var reactionCounts = await _context.CommunityReactions.AsNoTracking()
                .Where(reaction => postIds.Contains(reaction.PostId))
                .GroupBy(reaction => reaction.PostId)
                .Select(group => new { PostId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.PostId, group => group.Count);
            var commentCounts = await _context.CommunityComments.AsNoTracking()
                .Where(comment => postIds.Contains(comment.PostId) && !comment.IsHidden)
                .GroupBy(comment => comment.PostId)
                .Select(group => new { PostId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.PostId, group => group.Count);
            var shareCounts = await _context.CommunityShares.AsNoTracking()
                .Where(share => postIds.Contains(share.PostId))
                .GroupBy(share => share.PostId)
                .Select(group => new { PostId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.PostId, group => group.Count);
            var myReactions = string.IsNullOrWhiteSpace(currentUserId)
                ? new Dictionary<int, CommunityReactionType>()
                : await _context.CommunityReactions.AsNoTracking()
                    .Where(reaction => postIds.Contains(reaction.PostId) && reaction.UserId == currentUserId)
                    .ToDictionaryAsync(reaction => reaction.PostId, reaction => reaction.Type);

            var userIds = posts.Select(post => post.AuthorUserId)
                .Concat(comments.Select(comment => comment.AuthorUserId))
                .Append(currentUserId ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            var profiles = await GetProfileNamesAsync(userIds);
            var cards = posts.Select(post => new CommunityPostCardViewModel
            {
                Id = post.Id,
                AuthorUserId = post.AuthorUserId,
                AuthorName = profiles.GetValueOrDefault(post.AuthorUserId) ?? "Community member",
                AuthorInitials = CommunityProfilePolicy.GetInitials(profiles.GetValueOrDefault(post.AuthorUserId) ?? "Community member"),
                AuthorRole = "Community member",
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAtUtc = post.CreatedAtUtc,
                ReactionCount = reactionCounts.GetValueOrDefault(post.Id),
                CommentCount = commentCounts.GetValueOrDefault(post.Id),
                ShareCount = shareCounts.GetValueOrDefault(post.Id),
                MyReaction = myReactions.TryGetValue(post.Id, out var myReaction) ? myReaction : null,
                Comments = comments.Where(comment => comment.PostId == post.Id)
                    .OrderBy(comment => comment.CreatedAtUtc)
                    .Select(comment => new CommunityCommentCardViewModel
                    {
                        Id = comment.Id,
                        AuthorUserId = comment.AuthorUserId,
                        AuthorName = profiles.GetValueOrDefault(comment.AuthorUserId) ?? "Community member",
                        AuthorInitials = CommunityProfilePolicy.GetInitials(profiles.GetValueOrDefault(comment.AuthorUserId) ?? "Community member"),
                        Content = comment.Content,
                        CreatedAtUtc = comment.CreatedAtUtc
                    }).ToList(),
                ShareId = EncodeShareId(post.Id)
            }).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.IsSharedPost = !string.IsNullOrWhiteSpace(shared);
            return View(cards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> Create(CreateCommunityPostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["CommunityError"] = "Posts must contain text and be no longer than 3,000 characters.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!string.IsNullOrWhiteSpace(model.ImageUrl) && !IsSafeImageUrl(model.ImageUrl))
            {
                TempData["CommunityError"] = "Use an HTTPS image URL or leave the image field blank.";
                return RedirectToAction(nameof(Index));
            }

            var post = new CommunityPost
            {
                AuthorUserId = userId,
                Content = model.Content.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.CommunityPosts.Add(post);
            await _context.SaveChangesAsync();
            TempData["CommunityMessage"] = "Your post is live.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> Comment(CommunityCommentInputViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!ModelState.IsValid)
            {
                TempData["CommunityError"] = "Comments must contain text and be no longer than 1,000 characters.";
                return RedirectToAction(nameof(Index));
            }

            var postExists = await _context.CommunityPosts.AnyAsync(p => p.Id == model.PostId && !p.IsHidden);
            if (!postExists)
                return NotFound();

            _context.CommunityComments.Add(new CommunityComment
            {
                PostId = model.PostId,
                AuthorUserId = userId,
                Content = model.Content.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["CommunityMessage"] = "Comment added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> React(int postId, CommunityReactionType reaction = CommunityReactionType.Like)
        {
            if (!Enum.IsDefined(reaction))
                return BadRequest();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            var postExists = await _context.CommunityPosts.AnyAsync(p => p.Id == postId && !p.IsHidden);
            if (!postExists)
                return NotFound();

            var existing = await _context.CommunityReactions.FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);
            if (existing == null)
            {
                _context.CommunityReactions.Add(new CommunityReaction
                {
                    PostId = postId,
                    UserId = userId,
                    Type = reaction,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else if (existing.Type == reaction)
            {
                _context.CommunityReactions.Remove(existing);
            }
            else
            {
                existing.Type = reaction;
                existing.CreatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> Share(int postId)
        {
            var postExists = await _context.CommunityPosts.AnyAsync(p => p.Id == postId && !p.IsHidden);
            if (!postExists)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            var share = await _context.CommunityShares.FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId);
            if (share == null)
            {
                _context.CommunityShares.Add(new CommunityShare
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            var sharedId = EncodeShareId(postId);
            TempData["ShareUrl"] = Url.Action(nameof(Index), "Community", new { shared = sharedId }, Request.Scheme);
            TempData["CommunityMessage"] = "Share link created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> HidePost(int postId)
        {
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return NotFound();
            var userId = _userManager.GetUserId(User);
            if (userId == null || !CommunityProfilePolicy.CanModerate(post, userId, User.IsInRole("Admin")))
                return Forbid();
            post.IsHidden = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("community-write")]
        public async Task<IActionResult> HideComment(int commentId)
        {
            var comment = await _context.CommunityComments.FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
                return NotFound();
            var userId = _userManager.GetUserId(User);
            if (userId == null || !CommunityProfilePolicy.CanModerate(comment, userId, User.IsInRole("Admin")))
                return Forbid();
            comment.IsHidden = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<Dictionary<string, string>> GetProfileNamesAsync(List<string> userIds)
        {
            if (userIds.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users.AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new { user.Id, user.Email, user.UserName })
                .ToListAsync();
            var workers = await _context.Workers.AsNoTracking()
                .Where(worker => worker.UserId != null && userIds.Contains(worker.UserId))
                .Select(worker => new { worker.UserId, worker.FirstName, worker.LastName })
                .ToListAsync();
            var clients = await _context.Clients.AsNoTracking()
                .Where(client => client.UserId != null && userIds.Contains(client.UserId))
                .Select(client => new { client.UserId, client.FirstName, client.LastName })
                .ToListAsync();

            return users.ToDictionary(
                user => user.Id,
                user => CommunityProfilePolicy.GetDisplayName(
                    new ApplicationUser { Email = user.Email, UserName = user.UserName },
                    workers.FirstOrDefault(worker => worker.UserId == user.Id) is { } worker
                        ? $"{worker.FirstName} {worker.LastName}"
                        : null,
                    clients.FirstOrDefault(client => client.UserId == user.Id) is { } client
                        ? $"{client.FirstName} {client.LastName}"
                        : null));
        }

        private static bool IsSafeImageUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && (uri.HostNameType == UriHostNameType.Dns || uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6);
        }

        private static string EncodeShareId(int postId)
        {
            var bytes = BitConverter.GetBytes(postId);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static int? DecodeShareId(string shareId)
        {
            try
            {
                var normalized = shareId.Replace('-', '+').Replace('_', '/');
                normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
                var bytes = Convert.FromBase64String(normalized);
                return bytes.Length == sizeof(int) ? BitConverter.ToInt32(bytes) : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private sealed class CommunityCommentFeedRow
        {
            public int Id { get; set; }
            public int PostId { get; set; }
            public string AuthorUserId { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
