using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Application.DTOs.Auth;
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
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger<AuthController> logger,
        IEmailService emailService,
        IConfiguration configuration,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>
    /// Check if an email is already registered
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
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var validationError = await _registerValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            return BadRequest(new { message = "Email is already registered." });
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

        // Assign default role (Customer)
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

        _logger.LogInformation("User registered successfully: {Email}", request.Email);

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
    /// Sign in
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var validationError = await _loginValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Incorrect password for user: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Get user roles
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        var roles = userRoles.Where(r => r != null).Cast<string>().ToArray();
        if (roles.Length == 0)
            roles = new[] { Roles.Customer };

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        _logger.LogInformation("Successful login for user: {Email}", request.Email);

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
    /// Get current user information
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
            .AsNoTracking()
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
    /// Change password
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
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user: {UserId}", userId);

        return Ok(new { message = "Password updated successfully." });
    }

    /// <summary>
    /// Request password recovery
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Always return OK for security (don't reveal if the email exists)
        if (user == null)
        {
            _logger.LogInformation("Recovery attempt for non-existent email: {Email}", request.Email);
            return Ok(new { message = "If the email exists, you will receive a recovery link." });
        }

        // Generate a cryptographically secure token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(tokenBytes);

        // Hash the token before storing (SHA256)
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        // Store the hashed token with 1-hour expiry
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        // Build the reset link
        var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        // Send reset email
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendPasswordResetAsync(user.Email!, resetLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.Id);
            }
        });

        _logger.LogInformation("Password recovery token generated for: {Email}", request.Email);

        return Ok(new { message = "If the email exists, you will receive a recovery link." });
    }

    /// <summary>
    /// Reset password using a token from the forgot-password email
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Hash the incoming token to compare with stored hash
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (resetToken == null)
        {
            _logger.LogWarning("Password reset attempt with invalid token");
            return BadRequest(new { message = "Invalid or expired reset token." });
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset attempt with expired token for user {UserId}", resetToken.UserId);
            return BadRequest(new { message = "Invalid or expired reset token." });
        }

        if (resetToken.UsedAt.HasValue)
        {
            _logger.LogWarning("Password reset attempt with already-used token for user {UserId}", resetToken.UserId);
            return BadRequest(new { message = "This reset token has already been used." });
        }

        var user = resetToken.User;
        if (user == null)
        {
            return BadRequest(new { message = "Invalid or expired reset token." });
        }

        // Update the password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        // Mark token as used
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

        return Ok(new { message = "Password has been reset successfully." });
    }
}

#region DTOs (controller-specific, not duplicated in Application layer)

public record ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = "";
}

public record ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; init; } = "";

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    public string NewPassword { get; init; } = "";
}

public record ResetPasswordWithTokenRequest
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; init; } = "";

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    public string NewPassword { get; init; } = "";
}

public record EmailCheckResponse
{
    public string Email { get; init; } = "";
    public bool Exists { get; init; }
}

#endregion
