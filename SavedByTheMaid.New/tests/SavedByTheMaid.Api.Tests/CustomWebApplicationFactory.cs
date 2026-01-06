using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Factory personalizada para tests de integración.
/// 
/// BUENA PRÁCTICA:
/// 1. Hereda de WebApplicationFactory<Program>
/// 2. Sobrescribe ConfigureWebHost (no ConfigureServices)
/// 3. Reemplaza el DbContext real por InMemory
/// 4. Configura el ambiente como "Testing"
/// 5. Se usa con IClassFixture para compartir entre tests
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Establecer ambiente de Testing
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 2. Remover el DbContext de producción (MySQL)
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                           d.ServiceType == typeof(ApplicationDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 3. Agregar DbContext con InMemory para tests
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            });

            // 4. Asegurar que la DB se crea y seed inicial se ejecuta
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // EnsureCreated crea el schema en memoria
            db.Database.EnsureCreated();
            
            // Seed de datos de prueba si es necesario
            SeedTestData(db);
        });
    }

    /// <summary>
    /// Seed de datos mínimos para que los tests funcionen
    /// </summary>
    private static void SeedTestData(ApplicationDbContext context)
    {
        // Crear roles básicos
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityRole 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Admin", 
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new Microsoft.AspNetCore.Identity.IdentityRole 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Employee", 
                    NormalizedName = "EMPLOYEE",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new Microsoft.AspNetCore.Identity.IdentityRole 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Customer", 
                    NormalizedName = "CUSTOMER",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );
            context.SaveChanges();
        }
    }
}
