using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SavedByTheMaid.Api.Auth;

namespace SavedByTheMaid.Api.Tests.Unit;

/// <summary>
/// Unit tests for JwtService - token generation and validation
/// </summary>
public class JwtServiceTests
{
    private readonly JwtService _sut;
    private readonly JwtSettings _settings;

    public JwtServiceTests()
    {
        _settings = new JwtSettings
        {
            Secret = "TestSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };
        _sut = new JwtService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var token = _sut.GenerateAccessToken("user-123", "test@example.com", new[] { "Customer" });

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var token = _sut.GenerateAccessToken("user-123", "test@example.com", new[] { "Admin", "Customer" });

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "user-123");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Customer");
        jwt.Issuer.Should().Be(_settings.Issuer);
        jwt.Audiences.Should().Contain(_settings.Audience);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresInConfiguredTime()
    {
        var before = DateTime.UtcNow;
        var token = _sut.GenerateAccessToken("user-123", "test@example.com", new[] { "Customer" });
        var after = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(before.AddMinutes(_settings.ExpirationMinutes - 1));
        jwt.ValidTo.Should().BeBefore(after.AddMinutes(_settings.ExpirationMinutes + 1));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        var token = _sut.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(token);
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        var token = _sut.GenerateAccessToken("user-123", "test@example.com", new[] { "Customer" });

        var principal = _sut.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be("user-123");
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var principal = _sut.ValidateToken("invalid.token.string");

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        var token = _sut.GenerateAccessToken("user-123", "test@example.com", new[] { "Customer" });
        var tamperedToken = token + "tampered";

        var principal = _sut.ValidateToken(tamperedToken);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WrongIssuer_ReturnsNull()
    {
        // Generate token with different settings
        var otherSettings = new JwtSettings
        {
            Secret = _settings.Secret,
            Issuer = "WrongIssuer",
            Audience = _settings.Audience,
            ExpirationMinutes = 60
        };
        var otherService = new JwtService(Options.Create(otherSettings));
        var token = otherService.GenerateAccessToken("user-123", "test@example.com", new[] { "Customer" });

        var principal = _sut.ValidateToken(token);

        principal.Should().BeNull();
    }
}
