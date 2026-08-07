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
            model.RazorpayConfigured = _razorpayService.IsConfigured;
            model.RazorpayKeyId = _configuration["Razorpay:KeyId"];
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == booking.ClientId);
            if (client != null && !string.IsNullOrWhiteSpace(client.PhoneNumber))
            {
                model.PhoneNumber = UpiPaymentHelper.NormalizeIndiaPhone(client.PhoneNumber);
                model.ClientUpiId = client.UpiId;
            }

            var merchantVpa = _configuration["Upi:MerchantVpa"] ?? "rsinghrahul402@ybl";
            var merchantName = _configuration["Upi:MerchantName"] ?? "Indian Worker Mandi";
            model.MerchantUpiId = merchantVpa;
            model.MerchantName = merchantName;
            model.UpiPayUri = UpiPaymentHelper.BuildUpiPayUri(
                merchantVpa,
                merchantName,
                model.OnlineAmount,
                $"Booking #{booking.BookingId}");
            model.UpiQrCodeUrl = UpiPaymentHelper.BuildQrCodeUrl(model.UpiPayUri);
            model.SupportedPaymentMethods = new[]
            {
                "UPI Apps (PhonePe, GPay, Paytm)",
                "Cards / Net Banking / Wallet",
                "Manual UPI QR"
            };
            model.MaxPayoutAmount = Math.Max(0, booking.TotalWage - booking.AmountPaidOnline - booking.AmountPaidToWorker);

            await _auditService.LogPaymentInitiationAsync(bookingId, booking.ClientId?.ToString() ?? "unknown", booking.TotalWage, "payment-portal", HttpContext);

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
                var normalizedPhone = UpiPaymentHelper.NormalizeIndiaPhone(model.PhoneNumber);
                var (success, message, otpCode, devMode) = await _otpService.SendOtpAsync(normalizedPhone, userId!, model.BookingId);

                if (success && !string.IsNullOrWhiteSpace(otpCode))
                {
                    var otp = new OtpVerification
                    {
                        BookingId = model.BookingId,
                        UserId = userId!,
                        PhoneNumber = normalizedPhone,
                        OtpCode = otpCode,
                        GeneratedAt = DateTime.UtcNow,
                        IsVerified = false,
                        AttemptsRemaining = 3
                    };
                    _context.OtpVerifications.Add(otp);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("OTP requested for booking {BookingId} (devMode={DevMode})", model.BookingId, devMode);
                    return Json(new
                    {
                        success = true,
                        message,
                        devMode,
                        devOtp = devMode ? otpCode : null
                    });
                }

                return Json(new { success = false, message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RequestOtp: {ex.Message}");
                return Json(new { success = false, message = "Error sending OTP" });
            }
        }

        /// <summary>
        /// Verify OTP without creating a Razorpay order (for direct UPI flow).
        /// </summary>
        [HttpPost]
        [Route("Payment/VerifyOtpOnly")]
        public async Task<IActionResult> VerifyOtpOnly([FromBody] VerifyOtpOnlyRequestViewModel model)
        {
            var booking = await GetClientBooking(model.BookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Unable to identify user. Please log in again." });

            var (otpValid, otpMessage) = await _otpService.VerifyOtpAsync(userId, model.BookingId, model.OtpCode, _context);
            if (!otpValid)
                return Json(new { success = false, message = otpMessage });

            return Json(new { success = true, message = otpMessage });
        }

        /// <summary>
        /// Client pays via direct UPI to merchant VPA; admin approves later.
        /// </summary>
        [HttpPost]
        [Route("Payment/SubmitUpiPayment")]
        public async Task<IActionResult> SubmitUpiPayment([FromBody] SubmitUpiPaymentRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Please confirm your payment details." });

            var booking = await GetClientBooking(model.BookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Unable to identify user. Please log in again." });

            var pendingExists = await _context.UpiPaymentSubmissions.AnyAsync(u =>
                u.BookingId == model.BookingId && u.Status == UpiPaymentStatuses.Pending);
            if (pendingExists)
                return Json(new { success = false, message = "A UPI payment is already pending review for this booking." });

            try
            {
                var (otpValid, otpMessage) = await _otpService.VerifyOtpAsync(userId, model.BookingId, model.OtpCode, _context);
                if (!otpValid)
                    return Json(new { success = false, message = otpMessage });

                var merchantVpa = _configuration["Upi:MerchantVpa"] ?? "rsinghrahul402@ybl";
                var clientUpi = string.IsNullOrWhiteSpace(model.ClientUpiId) ? "not-provided" : model.ClientUpiId.Trim();
                var transactionRef = string.IsNullOrWhiteSpace(model.TransactionReference)
                    ? $"MANUAL-{model.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    : model.TransactionReference.Trim();

                var submission = new UpiPaymentSubmission
                {
                    BookingId = model.BookingId,
                    UserId = userId,
                    ClientUpiId = clientUpi,
                    TransactionReference = transactionRef,
                    Amount = model.Amount,
                    MerchantUpiId = merchantVpa,
                    Status = UpiPaymentStatuses.Pending,
                    SubmittedAt = DateTime.UtcNow
                };
                _context.UpiPaymentSubmissions.Add(submission);

                var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == booking.ClientId);
                if (client != null && clientUpi != "not-provided")
                {
                    client.UpiId = clientUpi;
                }

                booking.PaymentReference = $"UPI-PENDING-{model.TransactionReference.Trim()}";
                await _context.SaveChangesAsync();

                await _auditService.LogPaymentInitiationAsync(
                    model.BookingId,
                    booking.ClientId?.ToString() ?? "unknown",
                    model.Amount,
                    "upi-direct-pending",
                    HttpContext);

                return Json(new
                {
                    success = true,
                    message = "Payment submitted. Admin will verify in your UPI app and confirm the booking."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting UPI payment for booking {BookingId}", model.BookingId);
                return Json(new { success = false, message = "Could not submit UPI payment. Please try again." });
            }
        }

        /// <summary>
        /// Submit a payout instruction to the worker using UPI, card, or wallet.
        /// </summary>
        [HttpPost]
        [Route("Payment/SubmitPayout")]
        public async Task<IActionResult> SubmitPayout([FromBody] SubmitPayoutRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Please complete the payout details before sending money." });

            var booking = await GetClientBooking(model.BookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Unable to identify user. Please log in again." });

            var remainingToWorker = Math.Max(0, booking.TotalWage - booking.AmountPaidOnline - booking.AmountPaidToWorker);
            if (model.Amount <= 0 || model.Amount > remainingToWorker)
                return Json(new { success = false, message = "The payout amount is invalid." });

            try
            {
                var (otpValid, otpMessage) = await _otpService.VerifyOtpAsync(userId, model.BookingId, model.OtpCode, _context);
                if (!otpValid)
                    return Json(new { success = false, message = otpMessage });

                booking.AmountPaidToWorker += model.Amount;
                booking.PaymentReference = $"PAYOUT-{model.PayoutMethod.ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                UpdatePaymentStatus(booking);
                await _context.SaveChangesAsync();

                await _auditService.LogPaymentInitiationAsync(
                    model.BookingId,
                    booking.ClientId?.ToString() ?? "unknown",
                    model.Amount,
                    $"payout-{model.PayoutMethod.ToLowerInvariant()}",
                    HttpContext);

                await _auditService.LogPaymentCompletionAsync(
                    model.BookingId,
                    booking.PaymentReference,
                    booking.PaymentStatus,
                    $"Payout sent via {model.PayoutMethod} to {model.RecipientIdentifier}");

                return Json(new
                {
                    success = true,
                    message = $"Worker payout of ₹{model.Amount:F2} sent via {model.PayoutMethod}.",
                    payoutAmount = model.Amount,
                    newStatus = booking.PaymentStatus.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payout for booking {BookingId}", model.BookingId);
                return Json(new { success = false, message = "Could not send the payout. Please try again." });
            }
        }

        /// <summary>
        /// Step 3: Create Razorpay Order
        /// </summary>
        [HttpPost]
        [Route("Payment/CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid payment request. Please re-enter the OTP and try again." });

            if (model.Amount <= 0)
                return Json(new { success = false, message = "Invalid payment amount. Please refresh the page and try again." });

            var booking = await GetClientBooking(model.BookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == booking.ClientId);
            if (client == null)
                return Json(new { success = false, message = "Client not found" });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Unable to identify user. Please log in again." });

            try
            {
                var (otpValid, otpMessage) = await _otpService.VerifyOtpAsync(userId, model.BookingId, model.OtpCode, _context);
                if (!otpValid)
                    return Json(new { success = false, message = otpMessage });

                var result = await _razorpayService.CreateOrderAsync(
                    model.BookingId,
                    model.Amount,
                    client.Email ?? "",
                    client.PhoneNumber ?? ""
                );

                if ((bool)result["success"])
                {
                    // Save Razorpay order to database
                    var razorpayOrder = new RazorpayOrder
                    {
                        BookingId = model.BookingId,
                        RazorpayOrderId = (string)result["order_id"],
                        Amount = model.Amount,
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
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Payment verification failed. Please complete all required fields." });

            var booking = await GetClientBooking(model.BookingId);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Unable to identify user. Please log in again." });

            try
            {
                // Step 1: Verify Razorpay Signature (Security)
                var signatureValid = await _razorpayService.VerifyPaymentSignatureAsync(
                    model.RazorpayOrderId,
                    model.RazorpayPaymentId,
                    model.RazorpaySignature
                );

                if (!signatureValid)
                {
                    await _auditService.LogPaymentVerificationAsync(model.BookingId, model.RazorpayPaymentId, false, "Invalid signature");
                    _logger.LogWarning($"Invalid signature for payment {model.RazorpayPaymentId}");
                    return Json(new { success = false, message = "Payment signature verification failed. Possible fraud detected." });
                }

                var razorpayOrder = await _context.RazorpayOrders
                    .FirstOrDefaultAsync(ro => ro.RazorpayOrderId == model.RazorpayOrderId);

                if (razorpayOrder == null)
                {
                    await _auditService.LogPaymentVerificationAsync(model.BookingId, model.RazorpayPaymentId, false, "Razorpay order record not found");
                    return Json(new { success = false, message = "Payment order not found. Please contact support." });
                }

                razorpayOrder.RazorpayPaymentId = model.RazorpayPaymentId;
                razorpayOrder.RazorpaySignature = model.RazorpaySignature;
                razorpayOrder.Status = "verified";
                razorpayOrder.PaidAt = DateTime.UtcNow;

                // Update booking
                booking.AmountPaidOnline += razorpayOrder.Amount;
                booking.PaymentReference = model.RazorpayPaymentId;
                booking.Status = BookingStatus.Confirmed;
                UpdatePaymentStatus(booking);
                booking.PaidDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit logging
                await _auditService.LogPaymentCompletionAsync(
                    model.BookingId,
                    model.RazorpayPaymentId,
                    booking.PaymentStatus,
                    $"Razorpay verified: {model.RazorpayPaymentId}"
                );

                TempData["PaymentMessage"] = "✓ Payment successful! Your card details were NOT stored (PCI-DSS Compliant).";

                _logger.LogInformation($"Payment verified and processed for booking {model.BookingId}");
                return Json(new { success = true, message = "Payment verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying payment: {ex.Message}");
                await _auditService.LogPaymentVerificationAsync(model.BookingId, model.RazorpayPaymentId, false, ex.Message);
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

