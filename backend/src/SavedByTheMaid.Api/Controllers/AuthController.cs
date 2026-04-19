using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Api.Validators;
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
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordWithTokenRequest> _resetPasswordValidator;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger<AuthController> logger,
        IEmailService emailService,
        IConfiguration configuration,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordWithTokenRequest> resetPasswordValidator,
        IWebHostEnvironment env)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _env = env;
    }

    /// <summary>
    /// Check if an email is already registered
    /// </summary>
    [HttpGet("check-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<ActionResult<EmailCheckResponse>> CheckEmail([FromQuery] string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => new { u.PasswordHash })
            .FirstOrDefaultAsync(cancellationToken);
        stopwatch.Stop();

        // Normalize response time to prevent timing-based enumeration
        var elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed < 200)
            await Task.Delay((int)(200 - elapsed));

        return Ok(new EmailCheckResponse
        {
            Email = email,
            Exists = user != null,
            HasPassword = user != null && !string.IsNullOrEmpty(user.PasswordHash)
        });
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _registerValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
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
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);

        // Assign default role (Customer)
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Customer, cancellationToken);
        if (customerRole != null)
        {
            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = customerRole.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {Email}", request.Email);

        var roles = new[] { Roles.Customer };
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, accessToken, cancellationToken);

        // Set HttpOnly cookies
        SetAuthCookies(accessToken, refreshToken);

        return Created("/api/auth/me", new AuthResponse
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
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
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _loginValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
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
            .ToListAsync(cancellationToken);

        var roles = userRoles.Where(r => r != null).Cast<string>().ToArray();
        if (roles.Length == 0)
            roles = new[] { Roles.Customer };

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, accessToken, cancellationToken);

        // Set HttpOnly cookies
        SetAuthCookies(accessToken, refreshToken);

        _logger.LogInformation("Successful login for user: {Email}", request.Email);

        return Ok(new AuthResponse
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
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
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return NotFound();

        var userRoles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(cancellationToken);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.PhoneNumber,
            Roles = userRoles.Where(r => r != null).Cast<string>().ToArray()
        });
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _changePasswordValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return NotFound();

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        // Revoke all active refresh tokens so other sessions are forced to re-login
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.Revoked)
            .ToListAsync(cancellationToken);
        foreach (var t in activeTokens)
        {
            t.Revoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for user: {UserId}", userId);

        return Ok(new { message = "Password updated successfully." });
    }

    /// <summary>
    /// Request password recovery
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _forgotPasswordValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

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

        // Build the reset link
        var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        // Send email FIRST; only persist token if email succeeds (prevents orphaned tokens)
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _emailService.SendPasswordResetAsync(user.Email!, resetLink);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Password recovery token generated and email sent for: {Email}", request.Email);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to send password reset email for user {UserId}; token rolled back", user.Id);
            // Return 200 for security — don't reveal failure details
        }

        return Ok(new { message = "If the email exists, you will receive a recovery link." });
    }

    /// <summary>
    /// Reset password using a token from the forgot-password email
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _resetPasswordValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        // Hash the incoming token to compare with stored hash
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        // Atomically mark the token as used to prevent race conditions
        var now = DateTime.UtcNow;
        var rowsAffected = await _context.PasswordResetTokens
            .Where(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), cancellationToken);

        if (rowsAffected == 0)
        {
            _logger.LogWarning("Password reset attempt with invalid, expired, or already-used token");
            return BadRequest(new { message = "Invalid or expired reset token." });
        }

        // Load the token (now marked used) and its user
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        var user = resetToken?.User;
        if (user == null)
        {
            return BadRequest(new { message = "Invalid or expired reset token." });
        }

        // Update the password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

        // Revoke all refresh tokens for this user (force re-login)
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.Revoked)
            .ToListAsync(cancellationToken);
        foreach (var t in activeTokens)
        {
            t.Revoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(cancellationToken);

        // Clear auth cookies so the client must re-login with the new password
        ClearAuthCookies();

        return Ok(new { message = "Password has been reset successfully." });
    }

    /// <summary>
    /// Refresh access token using refresh token from HttpOnly cookie
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken = default)
    {
        // Tokens are exclusively read from HttpOnly cookies — no JSON body fallback
        // to prevent CSRF-style token exfiltration via body injection.
        var accessToken = Request.Cookies["accessToken"];
        var refreshTokenValue = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshTokenValue))
            return Unauthorized(new { message = "No refresh token provided." });

        // Find the stored refresh token
        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshTokenValue, cancellationToken);

        if (storedToken == null)
            return Unauthorized(new { message = "Invalid refresh token." });

        if (storedToken.Revoked)
        {
            // Possible token reuse attack — revoke all tokens for this user
            _logger.LogWarning("Revoked refresh token reuse detected for user {UserId}. Revoking all tokens.", storedToken.UserId);
            var allTokens = await _context.RefreshTokens
                .Where(t => t.UserId == storedToken.UserId && !t.Revoked)
                .ToListAsync(cancellationToken);
            foreach (var t in allTokens)
            {
                t.Revoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);
            ClearAuthCookies();
            return Unauthorized(new { message = "Token reuse detected. All sessions revoked." });
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            storedToken.Revoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            ClearAuthCookies();
            return Unauthorized(new { message = "Refresh token expired." });
        }

        var user = storedToken.User;
        if (user == null)
            return Unauthorized(new { message = "User not found." });

        // Get roles
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(cancellationToken);
        var roles = userRoles.Where(r => r != null).Cast<string>().ToArray();
        if (roles.Length == 0) roles = new[] { Roles.Customer };

        // Generate new token pair (rotation)
        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);
        var newRefreshToken = await RotateRefreshTokenAsync(storedToken, user.Id, newAccessToken, cancellationToken);

        // Set HttpOnly cookies
        SetAuthCookies(newAccessToken, newRefreshToken);

        return Ok(new AuthResponse
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                Roles = roles
            }
        });
    }

    /// <summary>
    /// Logout - revoke refresh token and clear cookies
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Revoke all active refresh tokens for this user
        if (!string.IsNullOrEmpty(userId))
        {
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.Revoked)
                .ToListAsync(cancellationToken);
            foreach (var t in activeTokens)
            {
                t.Revoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {UserId} logged out, {Count} refresh tokens revoked", userId, activeTokens.Count);
        }

        ClearAuthCookies();
        return Ok(new { message = "Logged out successfully." });
    }

    #region Private helpers

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        // Secure=false: nginx terminates SSL externally; backend runs on HTTP internally.
        // The browser communicates with nginx over HTTPS, so cookies are transmitted
        // securely at the network level even without the Secure flag on the cookie itself.
        Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationMinutes", 1440)),
            IsEssential = true,
            Path = "/api"
        });

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshExpirationDays", 30)),
            IsEssential = true,
            Path = "/api/auth"
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("accessToken", new CookieOptions { Path = "/api", Secure = false, SameSite = SameSiteMode.Lax });
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth", Secure = false, SameSite = SameSiteMode.Lax });
    }

    private async Task<string> RotateRefreshTokenAsync(RefreshToken oldToken, string userId, string newAccessToken, CancellationToken cancellationToken = default)
    {
        // Revoke old token
        oldToken.Revoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;

        // Generate new refresh token
        var newTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        // Extract JTI from new access token
        var jti = new JwtSecurityTokenHandler().ReadJwtToken(newAccessToken).Id ?? Guid.NewGuid().ToString();

        var newToken = new RefreshToken
        {
            Token = newTokenValue,
            JwtId = jti,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshExpirationDays", 30)),
            CreatedAt = DateTime.UtcNow
        };

        oldToken.ReplacedByToken = newTokenValue;
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);

        return newTokenValue;
    }

    private async Task<string> CreateRefreshTokenAsync(string userId, string accessToken, CancellationToken cancellationToken = default)
    {
        var jti = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Id ?? Guid.NewGuid().ToString();
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            JwtId = jti,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshExpirationDays", 30)),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tokenValue;
    }

    #endregion
}

#region DTOs (controller-specific, not duplicated in Application layer)

public record ForgotPasswordRequest
{
    public string Email { get; init; } = "";
}

public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = "";
    public string NewPassword { get; init; } = "";
}

public record ResetPasswordWithTokenRequest
{
    public string Token { get; init; } = "";
    public string NewPassword { get; init; } = "";
}

public record EmailCheckResponse
{
    public string Email { get; init; } = "";
    public bool Exists { get; init; }
    /// <summary>
    /// True only when an account exists AND has a usable password hash.
    /// Lets the client trigger an "email-first" login prompt instead of
    /// silently associating a guest booking to an existing account.
    /// </summary>
    public bool HasPassword { get; init; }
}

public record RefreshTokenRequest
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
}

#endregion
