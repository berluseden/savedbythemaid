namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Logs the envelope and returns. Used in test environments and as the
/// safety net when no provider is configured (so the app doesn't crash).
/// </summary>
public sealed class NoopEmailSender : IEmailSender
{
    private readonly ILogger<NoopEmailSender> _logger;

    public NoopEmailSender(ILogger<NoopEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email NOT sent (no provider configured): To={To}, Subject={Subject}",
            envelope.ToEmail, envelope.Subject);
        return Task.CompletedTask;
    }
}
