using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Tests for the authentication flow
///
/// BEST PRACTICE:
/// - One test = one specific behavior
/// - Descriptive names: Method_Scenario_ExpectedResult
/// - Arrange-Act-Assert clearly separated
/// </summary>
public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsToken()
    {
        // Arrange
        var request = new
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "TestPassword123!",
            Phone = "555-1234",
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content!.AccessToken.Should().NotBeNullOrEmpty();
        content.User.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "not-an-email",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "test@example.com",
            Password = "short" // Too short
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterRegister_ReturnsToken()
    {
        // Arrange
        var email = $"login_test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";

        // Register first
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        // Arrange - Register and get token
        var email = $"me_test_{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "TestPassword123!"
        });
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - Use HttpRequestMessage to avoid mutating shared _client headers
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Email.Should().Be(email);
    }
}

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public record UserDto(string Id, string Email, string? Phone, string[] Roles);
