using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.BackgroundServices;

/// <summary>
/// Background service que limpia soft reserves expirados cada 5 minutos
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
        var affected = await context.Database.ExecuteSqlAsync(
            $"UPDATE SoftReserves SET Status = {(int)SoftReserveStatus.Expired}, UpdatedAt = {now} WHERE ExpiresAt < {now} AND Status = {(int)SoftReserveStatus.Active}",
            cancellationToken);

        if (affected > 0)
        {
            _logger.LogInformation("Marked {Count} soft reserves as expired", affected);
        }
    }
}
