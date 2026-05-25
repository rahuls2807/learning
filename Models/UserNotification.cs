using System;
using System.Collections.Generic;

namespace WorkerBookingSystem.Models
{
    /// <summary>
    /// Real-time notifications for users
    /// </summary>
    public class UserNotification
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // BOOKING_CREATED, BOOKING_ACCEPTED, BOOKING_COMPLETED, PAYMENT_RECEIVED, REVIEW_RECEIVED, MESSAGE_RECEIVED
        public string NotificationType { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }

        // Related entity
        public int? BookingId { get; set; }
        public int? ReviewId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Priority: LOW, MEDIUM, HIGH
        public string Priority { get; set; } = "MEDIUM";

        // Action URL to navigate to
        public string ActionUrl { get; set; }
    }

    /// <summary>
    /// Direct messages between workers and clients
    /// </summary>
    public class Message
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public string Content { get; set; }

        // MESSAGE, RATING_OFFER, HELP_REQUEST, STATUS_UPDATE
        public string MessageType { get; set; } = "MESSAGE";

        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Attachments (JSON array of file URLs)
        public string Attachments { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Referral program tracking
    /// </summary>
    public class ReferralProgram
    {
        public int Id { get; set; }

        public string ReferrerId { get; set; }
        public ApplicationUser Referrer { get; set; }

        public string RefereeId { get; set; }
        public ApplicationUser Referee { get; set; }

        // Unique referral code
        public string ReferralCode { get; set; }

        // PENDING, ACTIVE, COMPLETED
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // First booking amount by referee
        public decimal FirstBookingAmount { get; set; }

        // Referral bonus earned
        public decimal BonusAmount { get; set; }

        // When bonus was credited
        public DateTime? BonusCreditedAt { get; set; }
    }

    /// <summary>
    /// Worker performance metrics and KPIs
    /// </summary>
    public class WorkerMetrics
    {
        public int Id { get; set; }

        public string WorkerId { get; set; }
        public ApplicationUser Worker { get; set; }

        // Review statistics
        public int TotalReviews { get; set; }
        public decimal AverageRating { get; set; }

        public int TotalBookingsCompleted { get; set; }
        public int TotalBookingsCancelled { get; set; }
        public int TotalBookingsActive { get; set; }

        // Financial metrics
        public decimal TotalEarnings { get; set; }
        public decimal AverageEarningsPerBooking { get; set; }

        // Reputation scores
        public decimal PunctualityScore { get; set; }
        public decimal QualityScore { get; set; }
        public decimal CommunicationScore { get; set; }
        public decimal ProfessionalismScore { get; set; }

        // Performance tier: BRONZE, SILVER, GOLD, PLATINUM
        public string PerformanceTier { get; set; } = "BRONZE";

        // Response time (minutes)
        public int AverageResponseTime { get; set; }

        // Cancellation rate (0-100)
        public decimal CancellationRate { get; set; }

        // Last updated
        public DateTime LastUpdatedAt { get; set; }

        // Badges/achievements
        public string Badges { get; set; } // JSON array
    }
}
