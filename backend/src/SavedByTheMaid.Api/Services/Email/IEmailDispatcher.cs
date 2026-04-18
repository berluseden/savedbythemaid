namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Background-friendly email dispatch. Callers enqueue and continue; the
/// actual transport call to <see cref="IEmailSender"/> happens in a
/// hosted worker so the user-facing request returns immediately.
///
/// Two implementations:
///   - <c>ChannelEmailDispatcher</c>  : in-memory bounded channel (default, zero infra)
///   - (future) HangfireEmailDispatcher : durable, multi-instance, dashboard
///
/// The interface is intentionally narrow so swapping the implementation
/// is a one-line change in <c>Program.cs</c> and call sites stay identical.
/// </summary>
public interface IEmailDispatcher
{
    /// <summary>
    /// Schedule an email for delivery. Returns once the envelope is queued
    /// (sub-millisecond for the channel impl). The actual SMTP/HTTP call
    /// runs out-of-band with retry + circuit breaker.
    /// </summary>
    ValueTask EnqueueAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default);
}
