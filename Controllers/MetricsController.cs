using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkerBookingSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MetricsController(WorkerBookingContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get worker performance metrics
        /// </summary>
        [HttpGet("worker/{workerId}")]
        public async Task<IActionResult> GetWorkerMetrics(int workerId)
        {
            var metrics = await _context.WorkerMetrics
                .FirstOrDefaultAsync(m => m.WorkerId == workerId.ToString());

            if (metrics == null)
            {
                metrics = await CalculateWorkerMetrics(workerId.ToString());
            }

            return Ok(metrics);
        }

        /// <summary>
        /// Get current user's metrics (if worker)
        /// </summary>
        [HttpGet("my-metrics")]
        public async Task<IActionResult> GetMyMetrics()
        {
            var userId = _userManager.GetUserId(User);
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == userId);

            if (worker == null)
                return BadRequest("User is not a worker");

            var metrics = await _context.WorkerMetrics
                .FirstOrDefaultAsync(m => m.WorkerId == userId);

            if (metrics == null)
            {
                metrics = await CalculateWorkerMetrics(userId);
            }

            return Ok(metrics);
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var userId = _userManager.GetUserId(User);

            // Check if user is worker
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
            if (worker != null)
            {
                return await GetWorkerDashboard(worker.WorkerId);
            }

            // Check if user is client
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null)
            {
                return await GetClientDashboard(client.ClientId);
            }

            return BadRequest("User role not identified");
        }

        private async Task<IActionResult> GetWorkerDashboard(int workerId)
        {
            var totalBookings = await _context.Bookings
                .Where(b => b.WorkerId == workerId)
                .CountAsync();

            var completedBookings = await _context.Bookings
                .Where(b => b.WorkerId == workerId && b.Status == BookingStatus.Completed)
                .CountAsync();

            var activeBookings = await _context.Bookings
                .Where(b => b.WorkerId == workerId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
                .CountAsync();

            var totalEarnings = await _context.Bookings
                .Where(b => b.WorkerId == workerId && b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.TotalWage) ?? 0;

            var reviews = await _context.WorkerReviews
                .Where(r => r.WorkerId == workerId)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerId == workerId);
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == worker.UserId);

            return Ok(new
            {
                userType = "WORKER",
                totalBookings,
                completedBookings,
                activeBookings,
                totalEarnings,
                averageRating,
                reviewCount = reviews.Count,
                walletBalance = wallet?.BalanceAmount ?? 0,
                loyaltyPoints = wallet?.LoyaltyPoints ?? 0
            });
        }

        private async Task<IActionResult> GetClientDashboard(int clientId)
        {
            var totalBookings = await _context.Bookings
                .Where(b => b.ClientId == clientId)
                .CountAsync();

            var completedBookings = await _context.Bookings
                .Where(b => b.ClientId == clientId && b.Status == BookingStatus.Completed)
                .CountAsync();

            var activeBookings = await _context.Bookings
                .Where(b => b.ClientId == clientId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress))
                .CountAsync();

            var totalSpent = await _context.Bookings
                .Where(b => b.ClientId == clientId && b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.TotalWage) ?? 0;

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == client.UserId);

            return Ok(new
            {
                userType = "CLIENT",
                totalBookings,
                completedBookings,
                activeBookings,
                totalSpent,
                walletBalance = wallet?.BalanceAmount ?? 0,
                loyaltyPoints = wallet?.LoyaltyPoints ?? 0
            });
        }

        private async Task<WorkerMetrics> CalculateWorkerMetrics(string workerId)
        {
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == workerId);
            if (worker == null)
                return null;

            var bookings = await _context.Bookings
                .Where(b => b.WorkerId == worker.WorkerId)
                .ToListAsync();

            var completedBookings = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
            var cancelledBookings = bookings.Where(b => b.Status == BookingStatus.Cancelled).ToList();

            var reviews = await _context.WorkerReviews
                .Where(r => r.WorkerId == worker.WorkerId)
                .ToListAsync();

            var metrics = new WorkerMetrics
            {
                WorkerId = workerId,
                TotalReviews = reviews.Count,
                AverageRating = reviews.Any() ? (decimal)reviews.Average(r => r.Rating) : 0,
                TotalBookingsCompleted = completedBookings.Count,
                TotalBookingsCancelled = cancelledBookings.Count,
                TotalBookingsActive = bookings.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress).Count(),
                TotalEarnings = (decimal)completedBookings.Sum(b => b.TotalWage),
                AverageEarningsPerBooking = completedBookings.Any() ? completedBookings.Sum(b => b.TotalWage) / completedBookings.Count : 0,
                CancellationRate = bookings.Any() ? (decimal)(cancelledBookings.Count * 100.0 / bookings.Count) : 0,
                LastUpdatedAt = DateTime.UtcNow,
                PerformanceTier = DeterminePerformanceTier(reviews.Count, reviews.Any() ? reviews.Average(r => r.Rating) : 0)
            };

            _context.WorkerMetrics.Add(metrics);
            await _context.SaveChangesAsync();

            return metrics;
        }

        private string DeterminePerformanceTier(int reviewCount, double avgRating)
        {
            if (reviewCount >= 50 && avgRating >= 4.5)
                return "PLATINUM";
            if (reviewCount >= 30 && avgRating >= 4.3)
                return "GOLD";
            if (reviewCount >= 10 && avgRating >= 4.0)
                return "SILVER";
            return "BRONZE";
        }
    }
}
