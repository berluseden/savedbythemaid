using Resend;

namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Resend-backed sender (https://resend.com).
///
/// Default for production. Free tier: 3,000 emails / month, 100 / day.
/// API key is read from `Email:Resend:ApiKey` (use user-secrets in dev,
/// env var `Email__Resend__ApiKey` in prod — never commit it).
///
/// `Email:FromEmail` defaults to `onboarding@resend.dev` (Resend's
/// shared sandbox sender — only delivers to the account owner). Switch
/// to `noreply@yourdomain.com` once the domain is DNS-verified in the
/// Resend dashboard.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailSender> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public ResendEmailSender(
        IResend resend,
        IConfiguration config,
        ILogger<ResendEmailSender> logger)
    {
        _resend = resend;
        _logger = logger;
        _fromEmail = config["Email:FromEmail"] ?? "onboarding@resend.dev";
        _fromName = config["Email:FromName"] ?? "SavedByTheMaid";
    }

    public async Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            From = $"{_fromName} <{_fromEmail}>",
            To = envelope.ToEmail,
            Subject = envelope.Subject,
            HtmlBody = envelope.HtmlBody,
        };

        if (!string.IsNullOrEmpty(envelope.PlainBody))
        {
            message.TextBody = envelope.PlainBody;
        }

        try
        {
            var response = await _resend.EmailSendAsync(message, cancellationToken);
            _logger.LogInformation(
                "Email sent via Resend: To={To}, Subject={Subject}, Id={Id}",
                envelope.ToEmail, envelope.Subject, response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Resend send failed: To={To}, Subject={Subject}",
                envelope.ToEmail, envelope.Subject);
            throw; // Re-throw so the queue (Hangfire) can retry
        }
    }
}
