# Worker Mandi Community

The Community is a professional-networking area for workers and clients. Its interface is original and uses Worker Mandi's identity; it is not affiliated with other networking products.

## What members can do

- Publish text updates with an optional HTTPS image URL.
- React with Like, Helpful, or Celebrate; members can change or remove their reaction.
- Comment on posts, see recent comments, and share a public link to a post.
- Start private direct conversations from a member's post, use the inbox, and chat live through SignalR. Message history is persisted and unread messages are marked read when a conversation opens.
- Continue to use booking-specific worker/client chat separately from community DMs.
- Remove one's own posts/comments; admins can moderate any post/comment.

The feed is paginated (20 posts per page), has a three-comment preview, and uses indexed aggregate reads. Actions are authenticated, CSRF-protected, and rate-limited. Messages/posts/comments have input length bounds. Public post links reveal only content the app already displays on the public feed; hide a post to remove it from public results.

## Database rollout

Apply the EF Core migrations once before enabling the updated app:

- `20261002123730_AddCommunitySocialFeed` creates posts, comments, reactions, and shares.
- `20261002124422_AddCommunityDirectMessages` creates private conversations and direct messages.

Development startup applies migrations automatically. Production startup does not; generate/run the migration SQL as a single release step, then deploy the application. Conversation pairs are normalized and uniquely indexed so the same two accounts reuse one inbox thread.

## Live chat configuration

The app maps the member DM SignalR hub at `/hubs/community-chat`. The optional `SignalR:Redis:ConnectionString` backplane setting described in `SCALING_AND_OPERATIONS.md` is needed for real-time delivery across multiple app instances. Without Redis (or managed SignalR), live fan-out is process-local; the database message remains saved and will appear on refresh.
