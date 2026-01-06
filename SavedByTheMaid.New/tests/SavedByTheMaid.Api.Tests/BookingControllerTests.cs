using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Tests de integración para el API de Booking
/// 
/// BUENA PRÁCTICA:
/// - IClassFixture<T> comparte la factory entre todos los tests de la clase
/// - Cada test usa CreateClient() para obtener un HttpClient limpio
/// - No se necesita IAsyncLifetime si no hay setup/teardown async
/// </summary>
public class BookingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BookingControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task Coverage_WithInvalidZip_ReturnsNotCovered()
    {
        // Act
        var response = await _client.GetAsync("/api/booking/coverage/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CoverageResponse>();
        content!.IsCovered.Should().BeFalse();
    }

    [Fact]
    public async Task CleaningPlaces_ReturnsEmptyList_WhenNoData()
    {
        // Act
        var response = await _client.GetAsync("/api/booking/cleaning-places");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ServiceTypes_ReturnsEmptyList_WhenNoData()
    {
        // Act
        var response = await _client.GetAsync("/api/booking/service-types");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Estimate_WithInvalidServiceType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            ServiceTypeId = 0, // Invalid
            Rooms = new List<object>(),
            AdditionalServiceIds = new List<int>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/booking/estimate", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Availability_WithInvalidZip_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            ZipCode = "abc", // Invalid format
            Date = DateTime.Today.AddDays(1),
            EstimatedMinutes = 120
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/booking/availability", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

public record HealthResponse(string Status, DateTime Timestamp, string Version);
public record CoverageResponse(bool IsCovered, int? ServiceAreaId, string? ServiceAreaName, string Message);
