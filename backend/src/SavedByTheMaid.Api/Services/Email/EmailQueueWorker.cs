namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Hosted service that drains the email queue and delivers envelopes via
/// the configured <see cref="IEmailSender"/>.
///
/// Resilience: HTTP-level retries + circuit breaker live in the sender's
/// HttpClient pipeline (Microsoft.Extensions.Http.Resilience). If a send
/// still fails after all retries, we log + swallow so the worker keeps
/// draining the channel — one bad envelope must not stall the whole queue.
///
/// Each envelope is processed in its own DI scope so transient services
/// (DbContext, etc.) are short-lived and don't leak across messages.
/// </summary>
public sealed class EmailQueueWorker : BackgroundService
{
    private readonly ChannelEmailDispatcher _dispatcher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueWorker> _logger;

    public EmailQueueWorker(
        IEmailDispatcher dispatcher,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueWorker> logger)
    {
        // The dispatcher must be the channel impl for the worker to consume it.
        // If a future Hangfire dispatcher replaces this, the worker is removed.
        _dispatcher = dispatcher as ChannelEmailDispatcher
            ?? throw new InvalidOperationException(
                "EmailQueueWorker requires ChannelEmailDispatcher. Update Program.cs registrations.");
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email queue worker started");

        await foreach (var envelope in _dispatcher.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(envelope, stoppingToken);
        }

        _logger.LogInformation("Email queue worker stopped");
    }

    private async Task ProcessAsync(EmailEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            await sender.SendAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown — re-throw so the loop exits.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Email queue: permanent failure after retries. To={To}, Subject={Subject}",
                envelope.ToEmail, envelope.Subject);
            // Swallow so one poison message doesn't kill the worker.
        }
    }
}
