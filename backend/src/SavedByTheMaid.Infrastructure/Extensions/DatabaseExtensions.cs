using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending migrations and runs initial seeds
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Verifying database connection...");

            // Verify connection
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogError("Cannot connect to the database");
                throw new InvalidOperationException("Cannot connect to the database");
            }

            logger.LogInformation("Verifying database schema...");

            // If the provider is not relational (e.g. InMemory in tests), use EnsureCreated
            if (!context.Database.IsRelational())
            {
                logger.LogWarning("Non-relational database provider detected. Using EnsureCreatedAsync() for test environment.");
                var created = await context.Database.EnsureCreatedAsync();
                if (created)
                {
                    logger.LogInformation("Database schema (InMemory) created successfully");
                }
                else
                {
                    logger.LogInformation("Database schema already exists (InMemory)");
                }

                // Avoid running relational-provider-specific SQL fixes
                return;
            }

            // Check if migrations are available (only for relational providers)
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            if (!appliedMigrations.Any() && !pendingMigrations.Any())
            {
                // No migrations found, use EnsureCreated
                logger.LogWarning("No migrations found. Creating schema with EnsureCreated()");
                var created = await context.Database.EnsureCreatedAsync();
                if (created)
                {
                    logger.LogInformation("Database schema created successfully");
                }
                else
                {
                    logger.LogInformation("Database schema already exists");
                }
            }
            else if (pendingMigrations.Any())
            {
                // There are pending migrations, apply them
                logger.LogInformation("Pending migrations: {Migrations}", string.Join(", ", pendingMigrations));
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");
            }
            else
            {
                logger.LogInformation("No pending migrations. Schema is up to date.");
            }

            // Apply manual fixes if the PaymentStatus column exists
            await ApplyManualFixesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying migrations");
            throw;
        }
    }

    private static async Task ApplyManualFixesAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Check if the PaymentStatus column exists
            var columnExists = await context.Database.ExecuteSqlRawAsync(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'ServiceOrders' 
                  AND COLUMN_NAME = 'PaymentStatus'") > 0;

            if (columnExists)
            {
                logger.LogWarning("PaymentStatus column detected, removing...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE ServiceOrders DROP COLUMN IF EXISTS PaymentStatus");
                logger.LogInformation("PaymentStatus column removed");
            }

            // Verify and create index IX_ServiceOrders_CreatedAt
            var hasCreatedAtIndex = await context.Database.ExecuteSqlRawAsync(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.STATISTICS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'ServiceOrders' 
                  AND INDEX_NAME = 'IX_ServiceOrders_CreatedAt'") > 0;

            if (!hasCreatedAtIndex)
            {
                logger.LogInformation("Creating index IX_ServiceOrders_CreatedAt...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE INDEX IX_ServiceOrders_CreatedAt 
                    ON ServiceOrders(CreatedAt DESC)");
                logger.LogInformation("Index IX_ServiceOrders_CreatedAt created");
            }

            // Verify and create composite index
            var hasCompositeIndex = await context.Database.ExecuteSqlRawAsync(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.STATISTICS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'ServiceOrders' 
                  AND INDEX_NAME = 'IX_ServiceOrders_OrderStatus_CreatedAt'") > 0;

            if (!hasCompositeIndex)
            {
                logger.LogInformation("Creating index IX_ServiceOrders_OrderStatus_CreatedAt...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE INDEX IX_ServiceOrders_OrderStatus_CreatedAt 
                    ON ServiceOrders(OrderStatus, CreatedAt DESC)");
                logger.LogInformation("Index IX_ServiceOrders_OrderStatus_CreatedAt created");
            }

            logger.LogInformation("Manual fixes applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying manual fixes");
            // Do not throw exception, allow the app to continue
        }
    }
}
