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
    public class WalletController : ControllerBase
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WalletController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get wallet balance for current user
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var userId = _userManager.GetUserId(User);
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new UserWallet
                {
                    UserId = userId,
                    BalanceAmount = 0,
                    TotalRecharged = 0,
                    TotalUsed = 0,
                    LoyaltyPoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                balance = wallet.BalanceAmount,
                totalRecharged = wallet.TotalRecharged,
                totalUsed = wallet.TotalUsed,
                loyaltyPoints = wallet.LoyaltyPoints
            });
        }

        /// <summary>
        /// Get wallet transaction history
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = _userManager.GetUserId(User);
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                return NotFound("Wallet not found");

            var transactions = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id)
                .CountAsync();

            return Ok(new { total, page, pageSize, transactions });
        }

        /// <summary>
        /// Recharge wallet with amount
        /// </summary>
        [HttpPost("recharge")]
        public async Task<IActionResult> RechargeWallet([FromBody] RechargeRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than 0");

            var userId = _userManager.GetUserId(User);
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new UserWallet
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            // Create transaction record
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = "CREDIT",
                TransactionType = "RECHARGE",
                Amount = request.Amount,
                OpeningBalance = wallet.BalanceAmount,
                ClosingBalance = wallet.BalanceAmount + request.Amount,
                Description = "Wallet recharge",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(transaction);

            // Update wallet
            wallet.BalanceAmount += request.Amount;
            wallet.TotalRecharged += request.Amount;
            wallet.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Recharge initiated",
                orderId = transaction.Id,
                amount = request.Amount,
                transactionId = transaction.Id
            });
        }

        /// <summary>
        /// Get wallet statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetWalletStatistics()
        {
            var userId = _userManager.GetUserId(User);
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                return NotFound();

            var monthlySpending = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id &&
                           t.Type == "DEBIT" &&
                           t.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                .SumAsync(t => t.Amount);

            var totalTransactions = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id)
                .CountAsync();

            return Ok(new
            {
                balance = wallet.BalanceAmount,
                totalTransactions,
                monthlySpending,
                loyaltyPoints = wallet.LoyaltyPoints,
                totalRecharged = wallet.TotalRecharged,
                totalUsed = wallet.TotalUsed
            });
        }
    }

    public class RechargeRequest
    {
        public decimal Amount { get; set; }
    }
}
