namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Stores refresh tokens in the database for validation and rotation.
/// Each token is linked to a user and a specific JWT (via JwtId).
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Revoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public ApplicationUser? User { get; set; }
}
