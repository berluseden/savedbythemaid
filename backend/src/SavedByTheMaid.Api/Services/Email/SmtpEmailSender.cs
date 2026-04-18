using System.Net;
using System.Net.Mail;

namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Fallback SMTP sender (Mailtrap, Workspace SMTP, self-hosted).
///
/// Uses System.Net.Mail.SmtpClient — Microsoft has marked this as legacy
/// for new code, but it works fine for dev sandboxes and emergency
/// fallback. For high-volume production prefer <see cref="ResendEmailSender"/>.
///
/// Selected when `Email:Provider` = "Smtp" (or omitted with `Email:SmtpHost`
/// configured but no Resend API key present).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.TryParse(_config["Email:SmtpPort"], out var port) ? port : 587;
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPassword = _config["Email:SmtpPassword"];
        var fromEmail = _config["Email:FromEmail"] ?? "noreply@savedbytemaid.com";
        var fromName = _config["Email:FromName"] ?? "SavedByTheMaid";

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
        {
            // Dev fallback: log only. Useful for unit/integration tests so the
            // pipeline does not require live SMTP credentials.
            _logger.LogInformation(
                "SMTP not configured — email skipped: To={To}, Subject={Subject}",
                envelope.ToEmail, envelope.Subject);
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPassword),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = envelope.Subject,
            Body = envelope.HtmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(envelope.ToEmail);

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation(
            "Email sent via SMTP: To={To}, Subject={Subject}",
            envelope.ToEmail, envelope.Subject);
    }
}
