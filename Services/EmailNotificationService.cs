using System;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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

            var displayName = string.IsNullOrWhiteSpace(options.FromName) ? "Catatan Keuangan" : options.FromName;

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromEmail, options.FromName),
                Subject = $"{displayName} - Token Reset Password"
            };

            var expiryDisplay = expiresAt.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID")) + " UTC";
            var plainTextBody =
$@"Halo,

Kami menerima permintaan untuk mereset password akun Anda.

Token reset Anda: {token}
Berlaku hingga: {expiryDisplay}

Salin token di atas ke aplikasi dan ikuti langkah reset password.
Jika Anda tidak merasa meminta reset password, abaikan email ini.

Terima kasih,
{displayName}";

            var htmlBody =
$@"<!DOCTYPE html>
<html lang=""id"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Token Reset Password</title>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f5f7fb; margin: 0; padding: 24px; color: #1f2937; }}
    .wrapper {{ max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 12px; box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08); overflow: hidden; }}
    .header {{ background-color: #2563eb; padding: 24px; color: #ffffff; }}
    .header h1 {{ margin: 0; font-size: 22px; }}
    .content {{ padding: 24px; line-height: 1.6; }}
    .token {{ display: inline-block; margin: 16px 0; padding: 12px 18px; background-color: #0f172a; color: #ffffff; font-weight: 600; letter-spacing: 0.12em; border-radius: 8px; font-size: 16px; }}
    .cta {{ display: inline-block; margin-top: 12px; padding: 12px 18px; background-color: #2563eb; color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 600; }}
    .cta:hover {{ background-color: #1d4ed8; }}
    .footer {{ padding: 18px 24px; background-color: #f8fafc; color: #64748b; font-size: 13px; }}
    p {{ margin: 0 0 12px 0; }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>{displayName}</h1>
    </div>
    <div class=""content"">
      <p>Halo,</p>
      <p>Kami menerima permintaan untuk mereset password akun Anda.</p>
      <p>Gunakan token berikut untuk melanjutkan proses reset password:</p>
      <div class=""token"">{token}</div>
      <p><strong>Berlaku hingga:</strong> {expiryDisplay}</p>
      <p>Salin token di atas ke aplikasi dan ikuti langkah reset password.</p>
      <p>Jika Anda tidak merasa meminta reset password, abaikan email ini.</p>
    </div>
    <div class=""footer"">
      <p>Terima kasih,</p>
      <p>{displayName}</p>
    </div>
  </div>
</body>
</html>";

            mailMessage.Body = htmlBody;
            mailMessage.IsBodyHtml = true;
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, MediaTypeNames.Text.Plain));

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
