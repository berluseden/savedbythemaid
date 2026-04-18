using System.Threading.Channels;

namespace SavedByTheMaid.Api.Services.Email;

/// <summary>
/// Bounded-channel queue. Holds envelopes in memory until the worker
/// drains them. Bounded so a misbehaving caller cannot blow up RAM.
///
/// Trade-off: if the process dies with envelopes still queued, those
/// emails are lost. Acceptable for pre-PMF SaaS where volume is low and
/// the alternative (Hangfire / Redis / SQS) adds infra. When durability
/// is required, swap this registration for <c>HangfireEmailDispatcher</c>
/// without touching call sites.
/// </summary>
public sealed class ChannelEmailDispatcher : IEmailDispatcher
{
    // 1024 envelopes is enough headroom for bursts without unbounded growth.
    public const int Capacity = 1024;

    private readonly Channel<EmailEnvelope> _channel = Channel.CreateBounded<EmailEnvelope>(
        new BoundedChannelOptions(Capacity)
        {
            // Wait briefly when full instead of dropping; the producer will
            // back-pressure rather than silently lose mail.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelReader<EmailEnvelope> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(envelope, cancellationToken);
}
