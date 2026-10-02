using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Hubs
{
    [Authorize]
    public class CommunityDirectChatHub : Hub
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityDirectChatHub(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = _userManager.GetUserId(Context.User!);
            var conversation = await _context.CommunityConversations.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == conversationId);
            if (conversation == null || !CommunityConversationPolicy.CanAccess(conversation, userId))
                throw new HubException("You do not have access to this conversation.");

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
            await _context.CommunityDirectMessages
                .Where(message => message.ConversationId == conversationId && message.SenderUserId != userId && message.ReadAtUtc == null)
                .ExecuteUpdateAsync(update => update.SetProperty(message => message.ReadAtUtc, DateTime.UtcNow));
        }

        public async Task SendMessage(int conversationId, string? content)
        {
            var userId = _userManager.GetUserId(Context.User!);
            var trimmed = content?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 2000)
                throw new HubException("Messages must contain 1 to 2,000 characters.");

            var conversation = await _context.CommunityConversations
                .FirstOrDefaultAsync(item => item.Id == conversationId);
            if (conversation == null || !CommunityConversationPolicy.CanAccess(conversation, userId))
                throw new HubException("You do not have access to this conversation.");

            var sentAt = DateTime.UtcNow;
            var message = new CommunityDirectMessage
            {
                ConversationId = conversationId,
                SenderUserId = userId!,
                Content = trimmed,
                SentAtUtc = sentAt
            };
            conversation.LastMessageAtUtc = sentAt;
            _context.CommunityDirectMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group(GroupName(conversationId)).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderUserId = message.SenderUserId,
                senderName = Context.User?.Identity?.Name ?? "Community member",
                content = message.Content,
                sentAtUtc = message.SentAtUtc
            });
        }

        private static string GroupName(int conversationId) => $"community-conversation:{conversationId}";
    }
}
