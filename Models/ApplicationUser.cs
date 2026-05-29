using Microsoft.AspNetCore.Identity;
using System;

namespace WorkerBookingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Referral Program
        public string ReferralCode { get; set; } = string.Empty;
        public string ReferredBy { get; set; } = string.Empty;

        // User Status
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }

        // KYC Status: PENDING, VERIFIED, REJECTED
        public string KycStatus { get; set; } = "PENDING";
        public DateTime? KycCompletedAt { get; set; }

        // Profile fields
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string BioDescription { get; set; } = string.Empty;

        // Preferences
        public bool NotificationsEnabled { get; set; } = true;
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool SmsNotificationsEnabled { get; set; } = true;

        // Account metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public int LoginCount { get; set; } = 0;

        // Subscription
        public string SubscriptionPlan { get; set; } = "FREE"; // FREE, PREMIUM, BUSINESS
        public DateTime? SubscriptionExpiresAt { get; set; }

        // Account status
        public bool IsActive { get; set; } = true;
        public bool IsBlocked { get; set; } = false;
        public string BlockReason { get; set; } = string.Empty;
    }
}


