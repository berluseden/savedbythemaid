using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.BackgroundServices;

/// <summary>
/// Background service that cleans up expired soft reserves and slot occupancies every 5 minutes.
/// Ensures that temporary slots are automatically released if the client does not complete checkout.
/// </summary>
public class SoftReserveCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SoftReserveCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SoftReserveCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SoftReserveCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SoftReserveCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await CleanupExpiredReserves(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SoftReserveCleanupService");
                // Continue running even if there is an error
            }
        }

        _logger.LogInformation("SoftReserveCleanupService stopped");
    }

    private async Task CleanupExpiredReserves(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Use a transaction to ensure consistency between both operations
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Delete expired SlotOccupancy records (SoftReserve with ExpiresAt < now)
            // This frees up slots so other users can book them
            var expiredSlots = await context.SlotOccupancies
                .Where(s => s.OccupancyType == OccupancyType.SoftReserve
                            && s.ExpiresAt != null
                            && s.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            if (expiredSlots.Count > 0)
            {
                context.SlotOccupancies.RemoveRange(expiredSlots);
                _logger.LogInformation("Deleted {Count} expired slot occupancies", expiredSlots.Count);
            }

            // 2. Mark SoftReserves as expired
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
                _logger.LogInformation("Marked {Count} soft reserves as expired", expiredReserves.Count);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error during cleanup transaction, rolled back");
            throw;
        }
    }
}
