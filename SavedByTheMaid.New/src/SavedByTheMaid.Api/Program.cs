using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Middleware;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Infrastructure;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Infrastructure.Extensions;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;

// Configurar Serilog antes de crear el builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Reemplazar el logger por defecto con Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/savedbythemaid-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ============================================
// 1. Configuración de servicios
// ============================================

// Infrastructure (DbContext, etc.) - pasa el environment para permitir testing
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

// JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher<SavedByTheMaid.Domain.Entities.ApplicationUser>, PasswordHasher<SavedByTheMaid.Domain.Entities.ApplicationUser>>();

// Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Autenticación JWT
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() 
    ?? new JwtSettings { Secret = "DefaultSecretKeyForDevelopmentOnly123456789!", Issuer = "SavedByTheMaid", Audience = "SavedByTheMaidApp" };

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// Autorización con políticas
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.EmployeeOrAdmin, policy => policy.RequireRole(Roles.Admin, Roles.Employee));
    options.AddPolicy(Policies.Authenticated, policy => policy.RequireAuthenticatedUser());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddFixedWindowLimiter("booking", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.AutoReplenishment = true;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new 
        { 
            message = "Demasiadas solicitudes. Por favor espera un momento." 
        }, token);
    };
});

// Background Services
builder.Services.AddHostedService<SavedByTheMaid.Api.BackgroundServices.SoftReserveCleanupService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000", "http://localhost:5173" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// YARP para proxy a Vite en desarrollo
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddReverseProxy()
        .LoadFromMemory(
            new[]
            {
                new Yarp.ReverseProxy.Configuration.RouteConfig
                {
                    RouteId = "spa",
                    ClusterId = "vite",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Path = "{**catch-all}"
                    }
                }
            },
            new[]
            {
                new Yarp.ReverseProxy.Configuration.ClusterConfig
                {
                    ClusterId = "vite",
                    Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
                    {
                        { "vite", new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5173" } }
                    }
                }
            });
}

var app = builder.Build();

// ============================================
// 2. Middleware Pipeline
// ============================================

// Global Exception Handler (primero para capturar todo)
app.UseGlobalExceptionHandler();

// Usar Serilog para request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("UserId", httpContext.User?.FindFirst("sub")?.Value ?? "anonymous");
    };
});

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Authentication & Authorization (en orden correcto)
app.UseAuthentication();
app.UseAuthorization();

// SPA Hosting - Servir archivos estáticos SOLO si no coinciden con /api/*
if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).AllowAnonymous();

// Fallback para SPA routing - AL FINAL, SOLO para rutas que NO empiecen con /api
if (app.Environment.IsDevelopment())
{
    // En desarrollo: proxy a Vite dev server
    app.MapReverseProxy();
}
else
{
    // En producción: custom fallback que ignora rutas /api
    app.Use(async (context, next) =>
    {
        // Si la ruta empieza con /api, dejar pasar al siguiente middleware (será 404 si no existe)
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            await next();
            return;
        }
        
        // Para cualquier otra ruta no encontrada, servir index.html (SPA routing)
        if (context.Response.StatusCode == 404)
        {
            context.Request.Path = "/index.html";
            await next();
        }
        else
        {
            await next();
        }
    });
}

// ============================================
// 3. Inicialización de Base de Datos
// ============================================

// Aplicar migraciones automáticamente
await app.Services.ApplyDatabaseMigrationsAsync();

// Seed de roles al iniciar
await SeedRolesAsync(app.Services);

// Seed de usuario admin
await SeedAdminUserAsync(app.Services);

// Seed de datos maestros (idempotente)
await SeedMasterDataAsync(app.Services);

// Log startup
Log.Information("SavedByTheMaid API iniciado - Entorno: {Environment}", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicación terminó inesperadamente");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// ============================================
// 3. Seed de datos iniciales
// ============================================
static async Task SeedRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Crear roles si no existen
        foreach (var roleName in Roles.All)
        {
            var roleExists = await context.Roles.AnyAsync(r => r.Name == roleName);
            if (!roleExists)
            {
                context.Roles.Add(new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
                logger.LogInformation("Rol creado: {Role}", roleName);
            }
        }
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al crear roles iniciales");
    }
}

static async Task SeedAdminUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const string adminEmail = "admin@savedbytemaid.com";
    const string adminPassword = "Admin123!";

    try
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "System",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                logger.LogInformation("Usuario admin creado: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Error al crear usuario admin: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al crear usuario admin");
    }
}

static async Task SeedMasterDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var seeder = new SavedByTheMaid.Infrastructure.Data.DataSeeder(context, 
            scope.ServiceProvider.GetRequiredService<ILogger<SavedByTheMaid.Infrastructure.Data.DataSeeder>>());
        await seeder.SeedAllAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al ejecutar seed de datos maestros");
        // No lanzar excepción para permitir que la app continúe
    }
}

// Clase parcial para WebApplicationFactory en tests
public partial class Program { }
