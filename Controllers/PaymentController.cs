using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Models.ViewModels;
using WorkerBookingSystem.Services;
using System.Security.Claims;

namespace WorkerBookingSystem.Controllers
{
    [Authorize(Roles = "Client")]
    public class PaymentController : Controller
    {
        private readonly WorkerBookingContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRazorpayPaymentService _razorpayService;
        private readonly IOtpService _otpService;
        private readonly IPaymentAuditService _auditService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;

        public PaymentController(
            WorkerBookingContext context,
            UserManager<ApplicationUser> userManager,
            IRazorpayPaymentService razorpayService,
            IOtpService otpService,
            IPaymentAuditService auditService,
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _razorpayService = razorpayService;
            _otpService = otpService;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Step 1: Display payment page with Razorpay form
        /// </summary>
        public async Task<IActionResult> Pay(int bookingId)
        {
            var booking = await GetClientBooking(bookingId);
            if (booking == null) return NotFound();

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                return RedirectToAction("MyBookings", "Client");
            }

            var model = ToPaymentViewModel(booking);
            model.RazorpayKeyId = _configuration["Razorpay:KeyId"];

            // Log payment initiation
            await _auditService.LogPaymentInitiationAsync(bookingId, booking.ClientId.ToString(), booking.TotalWage, "razorpay", HttpContext);

            return View(model);
        }

        /// <summary>
        /// Step 2: Request OTP for 2FA (RBI Requirement)
        /// </summary>
        [HttpPost]
        [Route("Payment/RequestOtp")]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequestViewModel model)
        {
            var booking = await GetClientBooking(model.BookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var userId = _userManager.GetUserId(User);

            try
            {
                // Generate and send OTP
                var result = await _otpService.SendOtpAsync(model.PhoneNumber, userId, model.BookingId);

                if (result.success)
                {
                    // Save OTP to database
                    var otp = new OtpVerification
                    {
                        BookingId = model.BookingId,
                        UserId = userId!,
                        PhoneNumber = model.PhoneNumber,
                        OtpCode = _otpService.GenerateOtp(),
                        GeneratedAt = DateTime.UtcNow,
                        IsVerified = false,
                        AttemptsRemaining = 3
                    };
                    _context.OtpVerifications.Add(otp);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"OTP requested for booking {model.BookingId}");
                    return Json(new { success = true, message = "OTP sent to your phone" });
                }

                return Json(new { success = false, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RequestOtp: {ex.Message}");
                return Json(new { success = false, message = "Error sending OTP" });
            }
        }

        /// <summary>
        /// Step 3: Create Razorpay Order
        /// </summary>
        [HttpPost]
        [Route("Payment/CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] dynamic data)
        {
            int bookingId = data.bookingId;
            decimal amount = data.amount;

            var booking = await GetClientBooking(bookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == booking.ClientId);
            if (client == null)
                return Json(new { success = false, message = "Client not found" });

            try
            {
                var result = await _razorpayService.CreateOrderAsync(
                    bookingId,
                    amount,
                    client.Email ?? "",
                    client.PhoneNumber ?? ""
                );

                if ((bool)result["success"])
                {
                    // Save Razorpay order to database
                    var razorpayOrder = new RazorpayOrder
                    {
                        BookingId = bookingId,
                        RazorpayOrderId = (string)result["order_id"],
                        Amount = amount,
                        CreatedAt = DateTime.UtcNow,
                        Status = "created"
                    };
                    _context.RazorpayOrders.Add(razorpayOrder);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, orderId = result["order_id"] });
                }

                return Json(new { success = false, message = result["error"] });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                return Json(new { success = false, message = "Error creating payment order" });
            }
        }

        /// <summary>
        /// Step 4: Verify Payment & OTP (CRITICAL SECURITY STEP)
        /// </summary>
        [HttpPost]
        [Route("Payment/VerifyPayment")]
        public async Task<IActionResult> VerifyPayment([FromBody] dynamic data)
        {
            int bookingId = data.bookingId;
            string razorpayOrderId = data.razorpayOrderId;
            string razorpayPaymentId = data.razorpayPaymentId;
            string razorpaySignature = data.razorpaySignature;
            string otpCode = data.otpCode;

            var booking = await GetClientBooking(bookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var userId = _userManager.GetUserId(User);

            try
            {
                // Step 1: Verify OTP (2FA)
                var (otpValid, otpMessage) = await _otpService.VerifyOtpAsync(userId, bookingId, otpCode, _context);
                if (!otpValid)
                    return Json(new { success = false, message = otpMessage });

                // Step 2: Verify Razorpay Signature (Security)
                var signatureValid = await _razorpayService.VerifyPaymentSignatureAsync(
                    razorpayOrderId,
                    razorpayPaymentId,
                    razorpaySignature
                );

                if (!signatureValid)
                {
                    await _auditService.LogPaymentVerificationAsync(bookingId, razorpayPaymentId, false, "Invalid signature");
                    _logger.LogWarning($"Invalid signature for payment {razorpayPaymentId}");
                    return Json(new { success = false, message = "Payment signature verification failed. Possible fraud detected." });
                }

                // Step 3: Update booking with payment details
                var razorpayOrder = await _context.RazorpayOrders
                    .FirstOrDefaultAsync(ro => ro.RazorpayOrderId == razorpayOrderId);

                if (razorpayOrder != null)
                {
                    razorpayOrder.RazorpayPaymentId = razorpayPaymentId;
                    razorpayOrder.RazorpaySignature = razorpaySignature;
                    razorpayOrder.Status = "verified";
                    razorpayOrder.PaidAt = DateTime.UtcNow;
                }

                // Update booking
                booking.AmountPaidOnline += razorpayOrder?.Amount ?? 0;
                booking.PaymentReference = razorpayPaymentId;
                booking.Status = BookingStatus.Confirmed;
                UpdatePaymentStatus(booking);
                booking.PaidDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit logging
                await _auditService.LogPaymentCompletionAsync(
                    bookingId,
                    razorpayPaymentId,
                    booking.PaymentStatus,
                    $"Razorpay verified: {razorpayPaymentId}"
                );

                TempData["PaymentMessage"] = "✓ Payment successful! Your card details were NOT stored (PCI-DSS Compliant).";

                _logger.LogInformation($"Payment verified and processed for booking {bookingId}");
                return Json(new { success = true, message = "Payment verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying payment: {ex.Message}");
                await _auditService.LogPaymentVerificationAsync(bookingId, razorpayPaymentId, false, ex.Message);
                return Json(new { success = false, message = "Error verifying payment" });
            }
        }

        /// <summary>
        /// Handle payment failure webhook from Razorpay
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [Route("Payment/PaymentFailed")]
        public async Task<IActionResult> PaymentFailed([FromBody] dynamic data)
        {
            try
            {
                string razorpayOrderId = data.razorpayOrderId;
                string errorDescription = data.errorDescription;

                var razorpayOrder = await _context.RazorpayOrders
                    .FirstOrDefaultAsync(ro => ro.RazorpayOrderId == razorpayOrderId);

                if (razorpayOrder != null)
                {
                    razorpayOrder.Status = "failed";
                    razorpayOrder.ErrorDescription = errorDescription;
                    _context.RazorpayOrders.Update(razorpayOrder);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning($"Payment failed for order {razorpayOrderId}: {errorDescription}");
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling payment failure: {ex.Message}");
                return Json(new { success = false });
            }
        }

        private async Task<Booking?> GetClientBooking(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null) return null;

            return await _context.Bookings
                .Include(b => b.Worker)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.ClientId == client.ClientId);
        }

        private static PaymentViewModel ToPaymentViewModel(Booking booking)
        {
            return new PaymentViewModel
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalWage,
                WorkerName = $"{booking.Worker?.FirstName} {booking.Worker?.LastName}".Trim(),
                AlreadyPaidOnline = booking.AmountPaidOnline,
                AlreadyPaidToWorker = booking.AmountPaidToWorker,
                BalanceDue = booking.TotalWage - booking.AmountPaidOnline - booking.AmountPaidToWorker,
                OnlineAmount = booking.TotalWage - booking.AmountPaidOnline - booking.AmountPaidToWorker
            };
        }

        private static void UpdatePaymentStatus(Booking booking)
        {
            var paid = booking.AmountPaidOnline + booking.AmountPaidToWorker;
            booking.PaymentStatus = paid <= 0
                ? PaymentStatus.Unpaid
                : paid >= booking.TotalWage
                    ? PaymentStatus.Paid
                    : PaymentStatus.PartiallyPaid;

            if (booking.PaymentStatus == PaymentStatus.Paid && booking.PaidDate == null)
            {
                booking.PaidDate = DateTime.Now;
            }
        }
    }
}

