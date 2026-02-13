using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Verificar si un email ya está registrado
    /// </summary>
    [HttpGet("check-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<ActionResult<EmailCheckResponse>> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        stopwatch.Stop();

        // Normalize response time to prevent timing-based enumeration
        var elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed < 200)
            await Task.Delay((int)(200 - elapsed));

        return Ok(new EmailCheckResponse
        {
            Email = email,
            Exists = exists
        });
    }

    /// <summary>
    /// Registrar nuevo usuario
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verificar si el email ya existe
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Intento de registro con email existente: {Email}", request.Email);
            return BadRequest(new { message = "El email ya está registrado." });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = request.Email,
            NormalizedUserName = request.Email.ToUpperInvariant(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            EmailConfirmed = false,
            PhoneNumber = request.Phone,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = request.Name?.Split(' ').FirstOrDefault(),
            LastName = request.Name?.Split(' ').Skip(1).FirstOrDefault()
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);

        // Asignar rol por defecto (Customer)
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Customer);
        if (customerRole != null)
        {
            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = customerRole.Id
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuario registrado exitosamente: {Email}", request.Email);

        var roles = new[] { Roles.Customer };
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                Phone = user.PhoneNumber,
                Roles = roles
            }
        });
    }

    /// <summary>
    /// Iniciar sesión
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning("Intento de login con email inexistente: {Email}", request.Email);
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Contraseña incorrecta para usuario: {Email}", request.Email);
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        // Obtener roles del usuario
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        var roles = userRoles.Where(r => r != null).Cast<string>().ToArray();
        if (roles.Length == 0)
            roles = new[] { Roles.Customer };

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        _logger.LogInformation("Login exitoso para usuario: {Email}", request.Email);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                Phone = user.PhoneNumber,
                Roles = roles
            }
        });
    }

    /// <summary>
    /// Obtener información del usuario actual
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Phone = user.PhoneNumber,
            Roles = userRoles.Where(r => r != null).Cast<string>().ToArray()
        });
    }

    /// <summary>
    /// Cambiar contraseña
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "La contraseña actual es incorrecta." });

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Contraseña cambiada para usuario: {UserId}", userId);

        return Ok(new { message = "Contraseña actualizada exitosamente." });
    }

    /// <summary>
    /// Solicitar recuperación de contraseña
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        // Siempre devolver OK por seguridad (no revelar si el email existe)
        if (user == null)
        {
            _logger.LogInformation("Intento de recuperación para email inexistente: {Email}", request.Email);
            return Ok(new { message = "Si el correo existe, recibirás un enlace de recuperación." });
        }

        // TODO: Generar token de reset y enviar email
        // Por ahora solo logueamos la solicitud
        _logger.LogInformation("Solicitud de recuperación de contraseña para: {Email}", request.Email);

        // Aquí iría:
        // 1. Generar token de reset con UserManager.GeneratePasswordResetTokenAsync
        // 2. Guardar token en BD con expiración
        // 3. Enviar email con enlace de reset

        return Ok(new { message = "Si el correo existe, recibirás un enlace de recuperación." });
    }
}

#region DTOs

public record ForgotPasswordRequest
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; init; } = "";
}

public record RegisterRequest
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
    public string Password { get; init; } = "";

    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Phone { get; init; }

    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string? Name { get; init; }
}

public record LoginRequest
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; init; } = "";
}

public record ChangePasswordRequest
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; init; } = "";

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
    public string NewPassword { get; init; } = "";
}

public record AuthResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}

public record UserDto
{
    public string Id { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
}

public record EmailCheckResponse
{
    public string Email { get; init; } = "";
    public bool Exists { get; init; }
}

#endregion
