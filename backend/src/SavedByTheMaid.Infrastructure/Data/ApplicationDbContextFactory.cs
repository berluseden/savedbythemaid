using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SavedByTheMaid.Infrastructure.Data;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> tooling
/// (e.g. <c>dotnet ef migrations add ...</c>). The connection is only
/// used to scaffold migrations — EF does not actually open it for the
/// model snapshot.
///
/// Connection string is resolved in this order:
///   1. <c>ConnectionStrings__DefaultConnection</c> env var (CI / Docker)
///   2. <c>EF_CONNECTION_STRING</c> env var (legacy / convenience)
///   3. A harmless placeholder (no real credentials)
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("EF_CONNECTION_STRING")
            ?? "Server=localhost;Port=3306;Database=savedbythemaid_design;User=ef;Password=design;";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySQL(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
