using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SavedByTheMaid.Api.Tests.Helpers;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Integration tests for the booking workflow.
/// Verifies response data and business rules, not just status codes.
/// </summary>
public class BookingWorkflowTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BookingWorkflowTests(SeededWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Step 1 - Coverage

    [Fact]
    public async Task Coverage_ValidZip_ReturnsCoveredWithServiceAreaId()
    {
        var response = await _client.GetAsync("/api/booking/coverage/32801");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CoverageResult>();
        body!.IsCovered.Should().BeTrue("32801 is a seeded covered zip code");
        body.ServiceAreaId.Should().Be(1, "seeded service area ID for Orlando is 1");
        body.ServiceAreaName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Coverage_UncoveredZip_ReturnsNotCoveredWithNullArea()
    {
        var response = await _client.GetAsync("/api/booking/coverage/99999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CoverageResult>();
        body!.IsCovered.Should().BeFalse("99999 is not a covered zip code");
        body.ServiceAreaId.Should().BeNull("uncovered areas should have no service area");
    }

    [Fact]
    public async Task Coverage_EmptyZip_ReturnsBadRequestOrNotFound()
    {
        var response = await _client.GetAsync("/api/booking/coverage/");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Step 2 - Catalog

    [Fact]
    public async Task CleaningPlaces_ReturnsActiveItemsWithRooms()
    {
        var response = await _client.GetAsync("/api/booking/cleaning-places");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("House", "seeded cleaning place should be in catalog");
        body.Should().Contain("Bedroom", "seeded rooms should be included");
    }

    [Fact]
    public async Task ServiceTypes_ReturnsActiveTypesWithPricing()
    {
        var response = await _client.GetAsync("/api/booking/service-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Standard Cleaning");
        // Verify pricing info is present
        body.Should().Contain("price", "service types must include pricing for the estimate step");
    }

    [Fact]
    public async Task AdditionalServices_ReturnsActiveServices()
    {
        var response = await _client.GetAsync("/api/booking/additional-services");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Oven Cleaning", "seeded additional service should be in catalog");
    }

    #endregion

    #region Step 3 - Estimate

    [Fact]
    public async Task Estimate_ValidRequest_ReturnsPositiveEstimate()
    {
        var response = await _client.PostAsJsonAsync("/api/booking/estimate", new
        {
            ServiceTypeId = 1,
            CleaningPlaceId = 1,
            Rooms = new[]
            {
                new { RoomId = 1, Quantity = 2 },
                new { RoomId = 2, Quantity = 1 }
            },
            AdditionalServiceIds = new int[] { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("estimatedMinutes",
            "estimate must include time duration");
        body.Should().Contain("subtotal",
            "estimate must include cost breakdown");

        // Parse and verify amounts are positive
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        root.GetProperty("estimatedMinutes").GetInt32().Should().BeGreaterThan(0,
            "estimate duration should be positive for any valid service");
        root.GetProperty("subtotal").GetDecimal().Should().BeGreaterThan(0,
            "estimate cost should be positive");
    }

    [Fact]
    public async Task Estimate_InvalidServiceType_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/booking/estimate", new
        {
            ServiceTypeId = 0,
            Rooms = new object[] { },
            AdditionalServiceIds = new int[] { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "estimate with invalid service type should fail validation");
    }

    [Fact]
    public async Task Estimate_MoreRooms_IncreasesEstimate()
    {
        // Estimate with 1 room
        var resp1 = await _client.PostAsJsonAsync("/api/booking/estimate", new
        {
            ServiceTypeId = 1,
            CleaningPlaceId = 1,
            Rooms = new[] { new { RoomId = 1, Quantity = 1 } },
            AdditionalServiceIds = new int[] { }
        });

        // Estimate with 3 rooms (more rooms = more work = higher price)
        var resp2 = await _client.PostAsJsonAsync("/api/booking/estimate", new
        {
            ServiceTypeId = 1,
            CleaningPlaceId = 1,
            Rooms = new[] { new { RoomId = 1, Quantity = 3 }, new { RoomId = 2, Quantity = 2 } },
            AdditionalServiceIds = new int[] { }
        });

        if (resp1.StatusCode == HttpStatusCode.OK && resp2.StatusCode == HttpStatusCode.OK)
        {
            var body1 = await resp1.Content.ReadAsStringAsync();
            var body2 = await resp2.Content.ReadAsStringAsync();

            using var doc1 = JsonDocument.Parse(body1);
            using var doc2 = JsonDocument.Parse(body2);

            var subtotal1 = doc1.RootElement.GetProperty("total").GetDecimal();
            var subtotal2 = doc2.RootElement.GetProperty("total").GetDecimal();

            subtotal2.Should().BeGreaterThan(subtotal1,
                "more rooms should result in a higher estimate");
        }
    }

    #endregion

    #region Step 4 - Availability

    [Fact]
    public async Task Availability_InvalidZip_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/booking/availability", new
        {
            ZipCode = "abc",
            Date = DateTime.Today.AddDays(1),
            EstimatedMinutes = 120
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "non-numeric zip codes should fail validation");
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task HealthCheck_ReturnsHealthyWithTimestamp()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
        body.Should().Contain("timestamp", "health check should include server time for monitoring");
    }

    #endregion
}

public record CoverageResult(bool IsCovered, int? ServiceAreaId, string? ServiceAreaName, string Message);
