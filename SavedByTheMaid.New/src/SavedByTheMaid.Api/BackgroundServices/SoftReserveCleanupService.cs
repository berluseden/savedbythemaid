using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.BackgroundServices;

/// <summary>
/// Background service que limpia soft reserves y slot occupancies expirados cada 5 minutos.
/// Garantiza que los slots temporales se liberen automáticamente si el cliente no completa el checkout.
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
                // Continuar ejecutando incluso si hay error
            }
        }

        _logger.LogInformation("SoftReserveCleanupService stopped");
    }

    private async Task CleanupExpiredReserves(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var softReserveType = (int)OccupancyType.SoftReserve;
        var expiredStatus = (int)SoftReserveStatus.Expired;
        var activeStatus = (int)SoftReserveStatus.Active;

        // 1. Eliminar SlotOccupancy expirados (SoftReserve con ExpiresAt < now)
        // Esto libera los slots para que otros usuarios puedan reservarlos
        var slotsDeleted = await context.Database.ExecuteSqlAsync(
            $"DELETE FROM SlotOccupancies WHERE OccupancyType = {softReserveType} AND ExpiresAt IS NOT NULL AND ExpiresAt < {now}",
            cancellationToken);

        if (slotsDeleted > 0)
        {
            _logger.LogInformation("Deleted {Count} expired slot occupancies", slotsDeleted);
        }

        // 2. Marcar SoftReserves como expirados
        var reservesExpired = await context.Database.ExecuteSqlAsync(
            $"UPDATE SoftReserves SET Status = {expiredStatus}, UpdatedAt = {now} WHERE ExpiresAt < {now} AND Status = {activeStatus}",
            cancellationToken);

        if (reservesExpired > 0)
        {
            _logger.LogInformation("Marked {Count} soft reserves as expired", reservesExpired);
        }
    }
}
