using System.ComponentModel.DataAnnotations;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Models.ViewModels
{
    public class CreateCommunityPostViewModel
    {
        [Required, StringLength(3000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ImageUrl { get; set; }
    }

    public class CommunityCommentInputViewModel
    {
        public int PostId { get; set; }

        [Required, StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public class CommunityPostCardViewModel
    {
        public int Id { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string AuthorInitials { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int ReactionCount { get; set; }
        public int CommentCount { get; set; }
        public int ShareCount { get; set; }
        public CommunityReactionType? MyReaction { get; set; }
        public IReadOnlyList<CommunityCommentCardViewModel> Comments { get; set; } = Array.Empty<CommunityCommentCardViewModel>();
        public string? ShareId { get; set; }
    }

    public class CommunityCommentCardViewModel
    {
        public int Id { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorInitials { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public class CommunityDirectMessageViewModel
    {
        public string SenderUserId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
    }

    public class CommunityInboxItemViewModel
    {
        public int ConversationId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserInitials { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAtUtc { get; set; }
        public int UnreadCount { get; set; }
    }

    public class CommunityDirectConversationViewModel
    {
        public int ConversationId { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserInitials { get; set; } = string.Empty;
        public IReadOnlyList<CommunityDirectMessageViewModel> Messages { get; set; } = Array.Empty<CommunityDirectMessageViewModel>();
    }

    public class SendCommunityDirectMessageViewModel
    {
        public int ConversationId { get; set; }

        [Required, StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }
}
