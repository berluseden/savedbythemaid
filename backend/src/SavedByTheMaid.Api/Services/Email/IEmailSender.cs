namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Low-level transport for outbound email. Implementations are pluggable
/// per environment (Resend in prod, SMTP / log-only in dev). Higher-level
/// services (booking confirmations, contact form, etc.) compose templates
/// and delegate the actual delivery here.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default);
}

public sealed record EmailEnvelope(
    string ToEmail,
    string Subject,
    string HtmlBody,
    string? PlainBody = null,
    string? ToName = null);
