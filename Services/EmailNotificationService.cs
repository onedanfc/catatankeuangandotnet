using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Options;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CatatanKeuanganDotnet.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly IOptions<SmtpOptions> _smtpOptions;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(IOptions<SmtpOptions> smtpOptions, ILogger<EmailNotificationService> logger)
        {
            _smtpOptions = smtpOptions;
            _logger = logger;
        }

        public async Task SendPasswordResetTokenAsync(string email, string token, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            var options = _smtpOptions.Value;

            if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.FromEmail))
            {
                _logger.LogWarning("SMTP configuration is incomplete. Password reset email for {Email} was not sent.", email);
                return;
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromEmail, options.FromName),
                Subject = "Token Reset Password",
                Body = $"Gunakan token berikut untuk reset password Anda: {token}\nToken berlaku hingga {expiresAt:dd MMM yyyy HH:mm} UTC.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(email);

            using var smtpClient = new SmtpClient(options.Host, options.Port)
            {
                EnableSsl = options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                smtpClient.Credentials = new NetworkCredential(options.Username, options.Password);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset token email sent to {Email}", email);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending password reset email to {Email} was cancelled.", email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                throw;
            }
        }
    }
}
