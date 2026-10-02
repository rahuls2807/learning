using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services
{
    public static class CommunityConversationPolicy
    {
        public static bool CanAccess(CommunityConversation conversation, string? userId)
        {
            return !string.IsNullOrWhiteSpace(userId)
                && (conversation.UserOneId == userId || conversation.UserTwoId == userId);
        }

        public static (string UserOneId, string UserTwoId) OrderPair(string firstId, string secondId)
        {
            return string.CompareOrdinal(firstId, secondId) < 0
                ? (firstId, secondId)
                : (secondId, firstId);
        }
    }
}
