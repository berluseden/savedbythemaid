using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup. Called from
    /// <c>Program.cs</c> after the host is built and before
    /// <see cref="DataSeeder"/> runs.
    ///
    /// Workflow:
    ///   1. Wait for the database to accept connections (handled by
    ///      <c>WaitForDatabaseAsync</c> in <c>Infrastructure.DependencyInjection</c>).
    ///   2. Apply any pending migrations (<see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>).
    ///   3. <see cref="DataSeeder"/> populates master data on top of the schema.
    ///
    /// For test environments using the InMemory provider we fall back to
    /// <c>EnsureCreatedAsync</c> because relational migrations don't apply.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // InMemory / non-relational test providers: just create the schema.
            if (!context.Database.IsRelational())
            {
                logger.LogInformation("Non-relational provider detected; using EnsureCreatedAsync");
                await context.Database.EnsureCreatedAsync();
                return;
            }

            // Verify connectivity (the caller already retries on cold-start
            // races, so a single check here is enough).
            if (!await context.Database.CanConnectAsync())
            {
                logger.LogError("Cannot connect to the database");
                throw new InvalidOperationException("Cannot connect to the database");
            }

            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("Database schema is up to date — no pending migrations");
                return;
            }

            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count,
                string.Join(", ", pending));
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying migrations");
            throw;
        }
    }
}
