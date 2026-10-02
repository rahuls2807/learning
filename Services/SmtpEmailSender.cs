using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WorkerBookingSystem.Services
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["Email:Smtp:Host"];
            var fromAddress = _configuration["Email:Smtp:FromAddress"];
            var fromName = _configuration["Email:Smtp:FromName"] ?? "Worker Mandi";
            var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
            var enableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true);
            var userName = _configuration["Email:Smtp:UserName"];
            var password = _configuration["Email:Smtp:Password"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new InvalidOperationException("Email delivery is not configured. Set Email:Smtp:Host and Email:Smtp:FromAddress.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName, Encoding.UTF8),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(email));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(userName))
            {
                client.Credentials = new NetworkCredential(userName, password ?? string.Empty);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Email delivered to {Recipient} with subject {Subject}.", email, subject);
        }
    }
}
