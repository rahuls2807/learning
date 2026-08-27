using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WalletController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("/Wallet")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var wallet = await EnsureWalletAsync(userId);
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var recentTransactions = await _context.WalletTransactions
                .Include(t => t.Booking)
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(12)
                .ToListAsync();

            var recipients = await _context.Users
                .Where(u => u.Id != userId && u.Email != null)
                .OrderByDescending(u => u.IsVerified)
                .ThenBy(u => u.Email)
                .Take(8)
                .Select(u => new WalletRecipientViewModel
                {
                    Email = u.Email ?? string.Empty,
                    DisplayName = u.UserName ?? u.Email ?? "User",
                    KycStatus = u.KycStatus
                })
                .ToListAsync();

            var monthlySpending = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id &&
                            t.Type == "DEBIT" &&
                            t.Status == "SUCCESS" &&
                            t.CreatedAt >= monthStart)
                .SumAsync(t => t.Amount);

            var monthlyCredits = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id &&
                            t.Type == "CREDIT" &&
                            t.Status == "SUCCESS" &&
                            t.CreatedAt >= monthStart)
                .SumAsync(t => t.Amount);

            var successfulTransfers = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id &&
                            t.TransactionType == "FUND_TRANSFER" &&
                            t.Status == "SUCCESS")
                .CountAsync();

            var protectedFunds = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id &&
                            t.TransactionType == "BOOKING_PAYMENT" &&
                            t.Status == "PENDING")
                .SumAsync(t => t.Amount);

            return View(new WalletDashboardViewModel
            {
                Wallet = wallet,
                RecentTransactions = recentTransactions,
                RecentRecipients = recipients,
                MonthlySpending = monthlySpending,
                MonthlyCredits = monthlyCredits,
                SuccessfulTransfers = successfulTransfers,
                ProtectedFunds = protectedFunds
            });
        }

        /// <summary>
        /// Get wallet balance for current user
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var userId = _userManager.GetUserId(User);
            var wallet = await EnsureWalletAsync(userId);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechargeWallet([FromBody] RechargeRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than 0");

            var userId = _userManager.GetUserId(User);
            var wallet = await EnsureWalletAsync(userId);
            var reference = CreateWalletReference("RECHARGE");

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = "CREDIT",
                TransactionType = "RECHARGE",
                Amount = request.Amount,
                OpeningBalance = wallet.BalanceAmount,
                ClosingBalance = wallet.BalanceAmount + request.Amount,
                Description = $"Wallet recharge via {NormalizePaymentMethod(request.PaymentMethod)}",
                Status = "SUCCESS",
                CreatedAt = DateTime.UtcNow,
                GatewayReference = reference
            };

            _context.WalletTransactions.Add(transaction);

            wallet.BalanceAmount += request.Amount;
            wallet.TotalRecharged += request.Amount;
            wallet.LoyaltyPoints += CalculateLoyaltyPoints(request.Amount);
            wallet.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Wallet recharged successfully",
                orderId = transaction.Id,
                amount = request.Amount,
                transactionId = transaction.Id,
                balance = wallet.BalanceAmount,
                loyaltyPoints = wallet.LoyaltyPoints,
                reference
            });
        }

        [HttpPost("transfer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferFunds([FromBody] TransferRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than 0");

            if (string.IsNullOrWhiteSpace(request.RecipientEmail))
                return BadRequest("Recipient email is required");

            var senderId = _userManager.GetUserId(User);
            var normalizedRecipientEmail = request.RecipientEmail.Trim().ToUpperInvariant();
            var recipient = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedRecipientEmail);

            if (recipient == null)
                return NotFound("Recipient account was not found");

            if (recipient.Id == senderId)
                return BadRequest("You cannot transfer funds to yourself");

            var senderWallet = await EnsureWalletAsync(senderId);
            if (senderWallet.BalanceAmount < request.Amount)
                return BadRequest("Insufficient wallet balance");

            var recipientWallet = await EnsureWalletAsync(recipient.Id);
            var reference = CreateWalletReference("TRANSFER");
            var now = DateTime.UtcNow;
            var note = string.IsNullOrWhiteSpace(request.Note) ? "Fund transfer" : request.Note.Trim();

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var senderLedger = new WalletTransaction
            {
                WalletId = senderWallet.Id,
                Type = "DEBIT",
                TransactionType = "FUND_TRANSFER",
                Amount = request.Amount,
                OpeningBalance = senderWallet.BalanceAmount,
                ClosingBalance = senderWallet.BalanceAmount - request.Amount,
                Description = $"Sent to {recipient.Email}: {note}",
                Status = "SUCCESS",
                CreatedAt = now,
                GatewayReference = reference
            };

            var recipientLedger = new WalletTransaction
            {
                WalletId = recipientWallet.Id,
                Type = "CREDIT",
                TransactionType = "FUND_TRANSFER",
                Amount = request.Amount,
                OpeningBalance = recipientWallet.BalanceAmount,
                ClosingBalance = recipientWallet.BalanceAmount + request.Amount,
                Description = $"Received from {User.Identity?.Name}: {note}",
                Status = "SUCCESS",
                CreatedAt = now,
                GatewayReference = reference
            };

            senderWallet.BalanceAmount -= request.Amount;
            senderWallet.TotalUsed += request.Amount;
            senderWallet.LastUpdated = now;

            recipientWallet.BalanceAmount += request.Amount;
            recipientWallet.TotalRecharged += request.Amount;
            recipientWallet.LastUpdated = now;

            _context.WalletTransactions.AddRange(senderLedger, recipientLedger);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Transfer completed",
                amount = request.Amount,
                recipient = recipient.Email,
                balance = senderWallet.BalanceAmount,
                reference
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

        private async Task<UserWallet> EnsureWalletAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("Authenticated user id was not found.");

            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet != null)
                return wallet;

            wallet = new UserWallet
            {
                UserId = userId,
                BalanceAmount = 0,
                TotalRecharged = 0,
                TotalUsed = 0,
                LoyaltyPoints = 0,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            };
            _context.UserWallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        private static string CreateWalletReference(string prefix)
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
        }

        private static string NormalizePaymentMethod(string? paymentMethod)
        {
            return string.IsNullOrWhiteSpace(paymentMethod) ? "UPI" : paymentMethod.Trim().ToUpperInvariant();
        }

        private static int CalculateLoyaltyPoints(decimal amount)
        {
            return (int)Math.Floor(amount / 100);
        }
    }

    public class RechargeRequest
    {
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class TransferRequest
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}
