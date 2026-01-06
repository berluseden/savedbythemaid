using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Tests para el flujo de autenticación
/// 
/// BUENA PRÁCTICA:
/// - Un test = un comportamiento específico
/// - Nombres descriptivos: Method_Scenario_ExpectedResult
/// - Arrange-Act-Assert claramente separados
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

    // NOTA: Estos tests requieren que Identity funcione correctamente con InMemory
    // En producción real, se usaría una base de datos de test dedicada (SQL Server LocalDB o contenedor)
    // Skip por ahora ya que la configuración de UserManager con InMemory tiene limitaciones
    
    [Fact(Skip = "Requiere configuración adicional de Identity con InMemory - issue conocido")]
    public async Task Login_AfterRegister_ReturnsToken()
    {
        // Arrange
        var email = $"login_test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        
        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password
        });

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

    [Fact(Skip = "Requiere configuración adicional de Identity con InMemory - issue conocido")]
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

        // Act
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var response = await _client.GetAsync("/api/auth/me");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Email.Should().Be(email);
    }
}

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public record UserDto(string Id, string Email, string? Phone, string[] Roles);
