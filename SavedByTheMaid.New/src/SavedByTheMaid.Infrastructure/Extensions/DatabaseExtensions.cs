using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Aplica migraciones pendientes y ejecuta seeds iniciales
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Verificando conexión a base de datos...");
            
            // Verificar conexión
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogError("No se puede conectar a la base de datos");
                throw new InvalidOperationException("No se puede conectar a la base de datos");
            }

            logger.LogInformation("Aplicando migraciones pendientes...");
            
            // Aplicar migraciones pendientes
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Migraciones pendientes: {Migrations}", string.Join(", ", pendingMigrations));
                await context.Database.MigrateAsync();
                logger.LogInformation("Migraciones aplicadas exitosamente");
            }
            else
            {
                logger.LogInformation("No hay migraciones pendientes");
            }

            // Aplicar correcciones manuales si la columna PaymentStatus existe
            await ApplyManualFixesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error aplicando migraciones");
            throw;
        }
    }

    private static async Task ApplyManualFixesAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Verificar si la columna PaymentStatus existe
            var columnExists = await context.Database.ExecuteSqlRawAsync(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'ServiceOrders' 
                  AND COLUMN_NAME = 'PaymentStatus'") > 0;

            if (columnExists)
            {
                logger.LogWarning("Columna PaymentStatus detectada, eliminando...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE ServiceOrders DROP COLUMN IF EXISTS PaymentStatus");
                logger.LogInformation("Columna PaymentStatus eliminada");
            }

            // Verificar y crear índice IX_ServiceOrders_CreatedAt
            var hasCreatedAtIndex = await context.Database.ExecuteSqlRawAsync(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.STATISTICS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'ServiceOrders' 
                  AND INDEX_NAME = 'IX_ServiceOrders_CreatedAt'") > 0;

            if (!hasCreatedAtIndex)
            {
                logger.LogInformation("Creando índice IX_ServiceOrders_CreatedAt...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE INDEX IX_ServiceOrders_CreatedAt 
                    ON ServiceOrders(CreatedAt DESC)");
                logger.LogInformation("Índice IX_ServiceOrders_CreatedAt creado");
            }

            // Verificar y crear índice compuesto
            var hasCompositeIndex = await context.Database.ExecuteSqlRawAsync(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.STATISTICS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'ServiceOrders' 
                  AND INDEX_NAME = 'IX_ServiceOrders_OrderStatus_CreatedAt'") > 0;

            if (!hasCompositeIndex)
            {
                logger.LogInformation("Creando índice IX_ServiceOrders_OrderStatus_CreatedAt...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE INDEX IX_ServiceOrders_OrderStatus_CreatedAt 
                    ON ServiceOrders(OrderStatus, CreatedAt DESC)");
                logger.LogInformation("Índice IX_ServiceOrders_OrderStatus_CreatedAt creado");
            }

            logger.LogInformation("Correcciones manuales aplicadas exitosamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error aplicando correcciones manuales");
            // No lanzar excepción, permitir que la app continúe
        }
    }
}
