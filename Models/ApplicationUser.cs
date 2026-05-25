using Microsoft.AspNetCore.Identity;
using System;

namespace WorkerBookingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Referral Program
        public string ReferralCode { get; set; }
        public string ReferredBy { get; set; } // UserId who referred this user

        // User Status
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }

        // KYC Status: PENDING, VERIFIED, REJECTED
        public string KycStatus { get; set; } = "PENDING";
        public DateTime? KycCompletedAt { get; set; }

        // Profile fields
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string ProfileImageUrl { get; set; }
        public string BioDescription { get; set; }

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
        public string BlockReason { get; set; }
    }
}


