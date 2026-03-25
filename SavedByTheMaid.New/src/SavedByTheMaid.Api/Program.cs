using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Middleware;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Application.Interfaces;
using SavedByTheMaid.Application.Validators;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Infrastructure;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Infrastructure.Extensions;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;

// Configure Serilog unless we're running tests in the 'Testing' environment
var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (!string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    // Configure Serilog before creating the builder
    try
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }
    catch (InvalidOperationException ex)
    {
        // During tests, Serilog initialization may be attempted multiple times.
        // Prevent this from stopping the test process; we'll fall back to the default logger.
        Console.Error.WriteLine($"Warning: Serilog bootstrap skipped: {ex.Message}");
    }
}

var builder = WebApplication.CreateBuilder(args);

// Replace the default logger with Serilog (except when running in Testing)
if (!string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    try
    {
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
    }
    catch (InvalidOperationException ex)
    {
        // Prevent duplicate Serilog initialization from stopping the tests.
        Console.Error.WriteLine($"Warning: Serilog host configuration skipped: {ex.Message}");
    }
}

// ============================================
// 1. Service Configuration
// ============================================

// Infrastructure (DbContext, etc.) - passes the environment to enable testing
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

// JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher<SavedByTheMaid.Domain.Entities.ApplicationUser>, PasswordHasher<SavedByTheMaid.Domain.Entities.ApplicationUser>>();

// Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Scheduling Service - conflict validation and SlotOccupancy management
builder.Services.AddScoped<ISchedulingService, SchedulingService>();

// Status History Service - status change auditing
builder.Services.AddScoped<IStatusHistoryService, StatusHistoryService>();

// Booking Service - pricing, confirmation, recurring meeting logic
builder.Services.AddScoped<IBookingService, BookingService>();

// Order Cancellation Service - shared cancellation logic
builder.Services.AddScoped<IOrderCancellationService, OrderCancellationService>();

// Application Layer Services
builder.Services.AddScoped<IOrderManagementService, SavedByTheMaid.Application.Services.OrderManagementService>();
builder.Services.AddScoped<IBookingApplicationService, SavedByTheMaid.Application.Services.BookingApplicationService>();
builder.Services.AddScoped<IContactService, SavedByTheMaid.Application.Services.ContactService>();

// FluentValidation - register all validators from the Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("JWT Secret must be configured in production. Set Jwt:Secret in appsettings or environment variables.");
    }
    jwtSettings = new JwtSettings { Secret = "DefaultSecretKeyForDevelopmentOnly123456789!", Issuer = "SavedByTheMaid", Audience = "SavedByTheMaidApp" };
}

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

// Authorization with policies
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

    options.AddFixedWindowLimiter("auth-sensitive", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.AutoReplenishment = true;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new 
        { 
            message = "Too many requests. Please wait a moment." 
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
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// YARP for proxying to Vite in development
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

// Global Exception Handler (first to catch everything)
app.UseGlobalExceptionHandler();

// Use Serilog for request logging (only if Serilog was configured, e.g. not in Testing)
if (!app.Environment.IsEnvironment("Testing"))
{
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
}

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HSTS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'");
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Authentication & Authorization (in correct order)
app.UseAuthentication();
app.UseAuthorization();

// SPA Hosting - Serve static files ONLY if they don't match /api/*
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

// Fallback for SPA routing - AT THE END, ONLY for routes that DON'T start with /api
if (app.Environment.IsDevelopment())
{
    // In development: proxy to Vite dev server
    app.MapReverseProxy();
}
else
{
    // In production: custom fallback that ignores /api routes
    app.Use(async (context, next) =>
    {
        await next();
        
        // If the route starts with /api, don't fallback (leave the natural 404)
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }
        
        // For any other route not found, serve index.html (SPA routing)
        if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
        {
            context.Request.Path = "/index.html";
            context.Response.StatusCode = 200;
            await next();
        }
    });
}

// ============================================
// 3. Database Initialization
// ============================================

// In the Testing environment we use the test configuration (InMemory, custom seeds),
// so we skip running the migration/seed that depend on Identity/production services.
if (!app.Environment.IsEnvironment("Testing"))
{
    // Apply migrations automatically
    await app.Services.ApplyDatabaseMigrationsAsync();

    // Seed roles on startup
    await SeedRolesAsync(app.Services);

    // Seed admin user
    await SeedAdminUserAsync(app.Services);

    // Seed master data (idempotent)
    await SeedMasterDataAsync(app.Services);
}

// Log startup
Log.Information("SavedByTheMaid API started - Environment: {Environment}", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// ============================================
// 3. Initial Data Seeding
// ============================================
static async Task SeedRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Create roles if they don't exist
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
                logger.LogInformation("Role created: {Role}", roleName);
            }
        }
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating initial roles");
    }
}

static async Task SeedAdminUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var adminEmail = configuration["AdminSeed:Email"] ?? "admin@savedbythemaid.com";
    var adminPassword = configuration["AdminSeed:Password"] ?? "Admin123!";
    
    if (adminPassword == "Admin123!")
    {
        logger.LogWarning("Using default admin password. Set AdminSeed:Password in configuration for production!");
    }

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
                logger.LogInformation("Admin user created: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Error creating admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating admin user");
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
        logger.LogError(ex, "Error running master data seed");
        // Do not throw exception to allow the app to continue
    }
}

// Partial class for WebApplicationFactory in tests
public partial class Program { }
