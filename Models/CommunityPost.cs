using System.ComponentModel.DataAnnotations;

namespace WorkerBookingSystem.Models
{
    public class CommunityPost
    {
        public int Id { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;

        [Required, StringLength(3000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public bool IsHidden { get; set; }
        public ApplicationUser? Author { get; set; }
        public ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
        public ICollection<CommunityReaction> Reactions { get; set; } = new List<CommunityReaction>();
        public ICollection<CommunityShare> Shares { get; set; } = new List<CommunityShare>();
    }

    public class CommunityComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;

        [Required, StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsHidden { get; set; }
        public CommunityPost? Post { get; set; }
        public ApplicationUser? Author { get; set; }
    }

    public class CommunityReaction
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public CommunityReactionType Type { get; set; } = CommunityReactionType.Like;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public CommunityPost? Post { get; set; }
        public ApplicationUser? User { get; set; }
    }

    public class CommunityShare
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public CommunityPost? Post { get; set; }
        public ApplicationUser? User { get; set; }
    }

    public class CommunityConversation
    {
        public int Id { get; set; }
        public string UserOneId { get; set; } = string.Empty;
        public string UserTwoId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;
        public ApplicationUser? UserOne { get; set; }
        public ApplicationUser? UserTwo { get; set; }
        public ICollection<CommunityDirectMessage> Messages { get; set; } = new List<CommunityDirectMessage>();
    }

    public class CommunityDirectMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderUserId { get; set; } = string.Empty;

        [Required, StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAtUtc { get; set; }
        public CommunityConversation? Conversation { get; set; }
        public ApplicationUser? Sender { get; set; }
    }

    public enum CommunityReactionType
    {
        Like,
        Helpful,
        Celebrate
    }
}
