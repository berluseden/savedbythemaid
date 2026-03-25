using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.BackgroundServices;

/// <summary>
/// Configuration options for <see cref="SoftReserveCleanupService"/>.
/// Bound from the <c>Monitoring</c> section in appsettings.json.
/// </summary>
public class SoftReserveCleanupOptions
{
    /// <summary>
    /// Interval in minutes between cleanup cycles. Defaults to 5 minutes.
    /// Configurable via <c>Monitoring:SoftReserveCleanupIntervalMinutes</c>.
    /// </summary>
    public int SoftReserveCleanupIntervalMinutes { get; set; } = 5;
}

/// <summary>
/// Background service that periodically cleans up expired soft reserves and
/// their associated slot occupancies.
/// <para>
/// <strong>Purpose:</strong> When a customer begins the booking checkout flow,
/// a "soft reserve" is created to temporarily hold an employee time slot. If the
/// customer abandons checkout, this service releases those holds so the slots
/// become available for other customers.
/// </para>
/// <para>
/// <strong>Cleanup operations (executed in a single transaction):</strong>
/// </para>
/// <list type="number">
///   <item>Deletes <c>SlotOccupancy</c> records of type <c>SoftReserve</c> whose
///         <c>ExpiresAt</c> has passed -- this frees up the employee's calendar.</item>
///   <item>Marks <c>SoftReserve</c> records with status <c>Active</c> as <c>Expired</c>
///         when their <c>ExpiresAt</c> has passed.</item>
/// </list>
/// <para>
/// <strong>Resilience:</strong> Individual cleanup cycle failures are logged and
/// do not stop the service from running future cycles. The service responds to
/// cancellation tokens for graceful shutdown.
/// </para>
/// <para>
/// <strong>Configuration:</strong> The cleanup interval is configurable via
/// <c>Monitoring:SoftReserveCleanupIntervalMinutes</c> in appsettings.json (default: 5).
/// </para>
/// <para>
/// <strong>Observability:</strong> The service exposes <see cref="LastCleanupUtc"/>,
/// <see cref="TotalSlotsCleanedUp"/>, <see cref="TotalReservesExpired"/>, and
/// <see cref="ConsecutiveFailures"/> for health check and monitoring integration.
/// </para>
/// </summary>
public class SoftReserveCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SoftReserveCleanupService> _logger;
    private readonly TimeSpan _interval;

    // ── Observability counters ───────────────────────────────────────────

    /// <summary>UTC timestamp of the last successful cleanup cycle.</summary>
    public DateTime? LastCleanupUtc { get; private set; }

    /// <summary>Cumulative number of expired slot occupancy records deleted.</summary>
    public long TotalSlotsCleanedUp { get; private set; }

    /// <summary>Cumulative number of soft reserves marked as expired.</summary>
    public long TotalReservesExpired { get; private set; }

    /// <summary>Number of consecutive cleanup failures. Resets to 0 on success.</summary>
    public int ConsecutiveFailures { get; private set; }

    public SoftReserveCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SoftReserveCleanupService> logger)
        : this(serviceProvider, logger, null)
    {
    }

    /// <summary>
    /// Initializes the cleanup service with optional configuration.
    /// </summary>
    /// <param name="serviceProvider">DI service provider for creating scoped services.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">
    /// Optional cleanup configuration. When <c>null</c>, the service resolves
    /// <see cref="SoftReserveCleanupOptions"/> from the DI container, falling
    /// back to a 5-minute default interval.
    /// </param>
    public SoftReserveCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SoftReserveCleanupService> logger,
        IOptions<SoftReserveCleanupOptions>? options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Resolve interval: constructor parameter > DI container > default (5 min)
        var intervalMinutes = options?.Value.SoftReserveCleanupIntervalMinutes ?? 5;
        if (intervalMinutes <= 0)
        {
            _logger.LogWarning(
                "Invalid SoftReserveCleanupIntervalMinutes ({Value}), using default of 5 minutes",
                intervalMinutes);
            intervalMinutes = 5;
        }
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SoftReserveCleanupService started with {IntervalMinutes}-minute interval",
            _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested during delay -- exit cleanly
                break;
            }

            try
            {
                await CleanupExpiredReserves(stoppingToken);
                ConsecutiveFailures = 0;
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested during cleanup -- exit cleanly
                break;
            }
            catch (Exception ex)
            {
                ConsecutiveFailures++;
                _logger.LogError(ex,
                    "Error in SoftReserveCleanupService (consecutive failure #{Count})",
                    ConsecutiveFailures);
                // Continue running -- do not let one failure stop future cleanups
            }
        }

        _logger.LogInformation(
            "SoftReserveCleanupService stopped. Lifetime stats: {SlotsDeleted} slots cleaned, {ReservesExpired} reserves expired",
            TotalSlotsCleanedUp, TotalReservesExpired);
    }

    /// <summary>
    /// Executes a single cleanup cycle: deletes expired slot occupancies and
    /// marks expired soft reserves. All operations run in a single database
    /// transaction for consistency.
    /// </summary>
    private async Task CleanupExpiredReserves(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Use a transaction to ensure consistency between both operations
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Delete expired SlotOccupancy records (SoftReserve type with ExpiresAt < now)
            //    This frees up employee time slots so other customers can book them
            var expiredSlots = await context.SlotOccupancies
                .Where(s => s.OccupancyType == OccupancyType.SoftReserve
                            && s.ExpiresAt != null
                            && s.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            if (expiredSlots.Count > 0)
            {
                context.SlotOccupancies.RemoveRange(expiredSlots);
                TotalSlotsCleanedUp += expiredSlots.Count;
                _logger.LogInformation("Deleted {Count} expired slot occupancies", expiredSlots.Count);
            }

            // 2. Mark active SoftReserves as expired (status change, not deletion)
            var expiredReserves = await context.SoftReserves
                .Where(r => r.ExpiresAt < now && r.Status == SoftReserveStatus.Active)
                .ToListAsync(cancellationToken);

            if (expiredReserves.Count > 0)
            {
                foreach (var reserve in expiredReserves)
                {
                    reserve.Status = SoftReserveStatus.Expired;
                    reserve.UpdatedAt = now;
                }
                TotalReservesExpired += expiredReserves.Count;
                _logger.LogInformation("Marked {Count} soft reserves as expired", expiredReserves.Count);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            LastCleanupUtc = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Error during cleanup transaction, rolled back");
            throw;
        }
    }
}
