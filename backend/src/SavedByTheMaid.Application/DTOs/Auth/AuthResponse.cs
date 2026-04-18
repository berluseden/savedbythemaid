namespace SavedByTheMaid.Application.DTOs.Auth;

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
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string[] Roles { get; init; } = [];
}
