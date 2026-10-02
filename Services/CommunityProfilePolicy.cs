using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    public static class CommunityProfilePolicy
    {
        public static string GetDisplayName(ApplicationUser? user, string? workerName = null, string? clientName = null)
        {
            if (!string.IsNullOrWhiteSpace(workerName))
                return workerName.Trim();
            if (!string.IsNullOrWhiteSpace(clientName))
                return clientName.Trim();
            if (!string.IsNullOrWhiteSpace(user?.Email))
                return user.Email.Split('@')[0];
            return "Community member";
        }

        public static string GetInitials(string displayName)
        {
            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => "CM",
                1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
                _ => string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[^1][0]))
            };
        }

        public static bool CanModerate(CommunityPost post, string userId, bool isAdmin)
        {
            return isAdmin || post.AuthorUserId == userId;
        }

        public static bool CanModerate(CommunityComment comment, string userId, bool isAdmin)
        {
            return isAdmin || comment.AuthorUserId == userId;
        }
    }
}
