using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReferralController : ControllerBase
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReferralController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get referral code for current user
        /// </summary>
        [HttpGet("my-code")]
        public async Task<IActionResult> GetMyReferralCode()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            // Generate referral code if doesn't exist
            if (string.IsNullOrEmpty(user.ReferralCode))
            {
                user.ReferralCode = GenerateReferralCode();
                await _userManager.UpdateAsync(user);
            }

            var activeReferrals = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId && r.Status == "COMPLETED")
                .CountAsync();

            var totalBonus = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId && r.Status == "COMPLETED")
                .SumAsync(r => r.BonusAmount);

            return Ok(new
            {
                referralCode = user.ReferralCode,
                activeReferrals,
                totalBonusEarned = totalBonus,
                referralLink = $"https://workermandi.com/register?ref={user.ReferralCode}"
            });
        }

        /// <summary>
        /// Get referral statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetReferralStatistics()
        {
            var userId = _userManager.GetUserId(User);

            var totalReferrals = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId)
                .CountAsync();

            var completedReferrals = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId && r.Status == "COMPLETED")
                .CountAsync();

            var pendingReferrals = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId && r.Status == "PENDING")
                .CountAsync();

            var totalBonusEarned = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId && r.Status == "COMPLETED")
                .SumAsync(r => (decimal?)r.BonusAmount) ?? 0;

            return Ok(new
            {
                totalReferrals,
                completedReferrals,
                pendingReferrals,
                totalBonusEarned
            });
        }

        /// <summary>
        /// Get my referrals list
        /// </summary>
        [HttpGet("my-referrals")]
        public async Task<IActionResult> GetMyReferrals([FromQuery] int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            var referrals = await _context.ReferralPrograms
                .Where(r => r.ReferrerId == userId)
                .Include(r => r.Referee)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(r => new
                {
                    r.Id,
                    r.ReferralCode,
                    refereeName = r.Referee.UserName,
                    refereeEmail = r.Referee.Email,
                    r.Status,
                    firstBookingAmount = r.FirstBookingAmount,
                    bonusAmount = r.BonusAmount,
                    r.CreatedAt,
                    r.CompletedAt
                })
                .ToListAsync();

            return Ok(referrals);
        }

        /// <summary>
        /// Register referral when referee completes first booking
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterReferral([FromBody] RegisterReferralRequest request)
        {
            var referrer = await _userManager.Users
                .FirstOrDefaultAsync(u => u.ReferralCode == request.ReferralCode);

            if (referrer == null)
                return BadRequest("Invalid referral code");

            var refereeId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(refereeId))
                return Unauthorized("User must be logged in");

            // Check if already referred
            var existing = await _context.ReferralPrograms
                .FirstOrDefaultAsync(r => r.RefereeId == refereeId && r.ReferrerId == referrer.Id);

            if (existing != null)
                return BadRequest("User already has an active referral");

            var referral = new ReferralProgram
            {
                ReferrerId = referrer.Id,
                RefereeId = refereeId,
                ReferralCode = request.ReferralCode,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _context.ReferralPrograms.Add(referral);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Referral registered successfully" });
        }

        /// <summary>
        /// Complete referral when referee completes first booking
        /// </summary>
        [HttpPost("complete/{bookingId}")]
        public async Task<IActionResult> CompleteReferral(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound("Booking not found");

            var clientUserId = booking.Client?.UserId;
            if (string.IsNullOrEmpty(clientUserId))
                return BadRequest("Booking has no client");

            var referral = await _context.ReferralPrograms
                .FirstOrDefaultAsync(r => r.RefereeId == clientUserId && r.Status != "COMPLETED");

            if (referral == null)
                return BadRequest("No active referral for this user");

            // Calculate bonus (5% of booking amount, max 500 INR)
            var bonusAmount = Math.Min(booking.TotalWage * 0.05m, 500);

            referral.Status = "COMPLETED";
            referral.FirstBookingAmount = booking.TotalWage;
            referral.BonusAmount = bonusAmount;
            referral.CompletedAt = DateTime.UtcNow;

            // Add bonus to referrer's wallet
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == referral.ReferrerId);

            if (wallet != null)
            {
                wallet.BalanceAmount += bonusAmount;
                wallet.LastUpdated = DateTime.UtcNow;

                var transaction = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    Type = "CREDIT",
                    TransactionType = "REFERRAL_BONUS",
                    Amount = bonusAmount,
                    OpeningBalance = wallet.BalanceAmount - bonusAmount,
                    ClosingBalance = wallet.BalanceAmount,
                    Description = $"Referral bonus for {referral.RefereeId}",
                    Status = "SUCCESS",
                    CreatedAt = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(transaction);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Referral completed", bonusAmount });
        }

        private string GenerateReferralCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 8)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }
    }

    public class RegisterReferralRequest
    {
        public string ReferralCode { get; set; }
    }
}
