using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Security tests to verify that admin endpoints require authentication
///
/// BEST PRACTICE:
/// - Reuse CustomWebApplicationFactory (do not duplicate configuration)
/// - Parameterized tests with [Theory] + [InlineData] for multiple endpoints
/// </summary>
public class AdminSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminSecurityTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/admin/orders")]
    [InlineData("/api/admin/employees")]
    [InlineData("/api/admin/cleaningplaces")]
    [InlineData("/api/admin/servicetypes")]
    [InlineData("/api/admin/additionalservices")]
    [InlineData("/api/admin/equipment")]
    [InlineData("/api/admin/pricemultipliers")]
    public async Task AdminEndpoints_WithoutAuth_ReturnsUnauthorized(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/admin/employees", "POST")]
    [InlineData("/api/admin/cleaningplaces", "POST")]
    public async Task AdminPostEndpoints_WithoutAuth_ReturnsUnauthorized(string endpoint, string method)
    {
        // Act
        var response = method == "POST" 
            ? await _client.PostAsJsonAsync(endpoint, new { })
            : await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BookingEndpoints_ArePublic()
    {
        // Act - these endpoints should be public
        var coverageResponse = await _client.GetAsync("/api/booking/coverage/10001");
        var placesResponse = await _client.GetAsync("/api/booking/cleaning-places");
        var typesResponse = await _client.GetAsync("/api/booking/service-types");
        
        // Assert - should not return 401
        coverageResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        placesResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        typesResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
