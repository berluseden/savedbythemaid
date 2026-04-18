using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SavedByTheMaid.Api.Tests.Helpers;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Tests for Admin CRUD endpoints.
/// Verifies data is actually persisted, updated, and soft-deleted correctly.
/// </summary>
public class AdminCrudTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly HttpClient _client;
    private string? _adminToken;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AdminCrudTests(SeededWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        if (_adminToken != null) return _adminToken;

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = TestDataSeeder.AdminEmail,
            Password = TestDataSeeder.TestPassword
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        _adminToken = auth!.AccessToken;
        return _adminToken;
    }

    #region CleaningPlaces CRUD

    [Fact]
    public async Task CleaningPlaces_GetAll_ReturnsSeededPlace()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(_client, "/api/admin/cleaningplaces", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("House", "seeded cleaning place should be returned");
    }

    [Fact]
    public async Task CleaningPlaces_Create_PersistsAndReturnsData()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces", token,
            new { Name = "Apartment", Description = "Studio apartment" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CleaningPlaceResponse>(JsonOpts);
        created!.Name.Should().Be("Apartment");
        created.Description.Should().Be("Studio apartment");
        created.IsActive.Should().BeTrue("new places should default to active");
        created.Id.Should().BeGreaterThan(0);

        // Verify we can read it back
        var getResp = await TestAuthHelper.GetAuthenticatedAsync(
            _client, $"/api/admin/cleaningplaces/{created.Id}", token);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResp.Content.ReadFromJsonAsync<CleaningPlaceResponse>(JsonOpts);
        fetched!.Name.Should().Be("Apartment");
    }

    [Fact]
    public async Task CleaningPlaces_Update_ChangesName()
    {
        var token = await GetAdminTokenAsync();

        // Create
        var createResp = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces", token,
            new { Name = "OldName", Description = "Before" });
        var created = await createResp.Content.ReadFromJsonAsync<IdResponse>();

        // Update
        var response = await TestAuthHelper.PutAuthenticatedAsync(
            _client, $"/api/admin/cleaningplaces/{created!.Id}", token,
            new { Name = "NewName", Description = "After", IsActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update persisted
        var getResp = await TestAuthHelper.GetAuthenticatedAsync(
            _client, $"/api/admin/cleaningplaces/{created.Id}", token);
        var fetched = await getResp.Content.ReadFromJsonAsync<CleaningPlaceResponse>(JsonOpts);
        fetched!.Name.Should().Be("NewName", "update should change the name");
        fetched.Description.Should().Be("After");
    }

    [Fact]
    public async Task CleaningPlaces_Delete_SoftDeletes_NoLongerVisible()
    {
        var token = await GetAdminTokenAsync();

        var createResp = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces", token,
            new { Name = "ToDelete", Description = "Will be deleted" });
        var created = await createResp.Content.ReadFromJsonAsync<IdResponse>();

        var response = await TestAuthHelper.DeleteAuthenticatedAsync(
            _client, $"/api/admin/cleaningplaces/{created!.Id}", token);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Soft-deleted items should not be found via GetById
        var getResp = await TestAuthHelper.GetAuthenticatedAsync(
            _client, $"/api/admin/cleaningplaces/{created.Id}", token);
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "soft-deleted places should not be returned by GetById");

        // Soft-deleted items should not appear in GetAll
        var allResp = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces", token);
        var allBody = await allResp.Content.ReadAsStringAsync();
        allBody.Should().NotContain("ToDelete",
            "soft-deleted places should not appear in the list");
    }

    [Fact]
    public async Task CleaningPlaces_AddRoom_PersistsWithCorrectValues()
    {
        var token = await GetAdminTokenAsync();

        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces/1/rooms", token,
            new { Name = "Kitchen", Description = "Full kitchen", BaseMinutes = 30, BasePrice = 25.00, DisplayOrder = 3 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var room = await response.Content.ReadFromJsonAsync<RoomResponse>(JsonOpts);
        room!.Name.Should().Be("Kitchen");
        room.BaseMinutes.Should().Be(30);
        room.BasePrice.Should().Be(25.00m);
        room.DisplayOrder.Should().Be(3);
        room.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CleaningPlaces_GetRooms_ReturnsOrderedByDisplayOrder()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces/1/rooms", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomResponse>>(JsonOpts);
        rooms.Should().NotBeEmpty("seeded place should have rooms");
        // Rooms should be ordered by DisplayOrder (ascending)
        rooms!.Should().BeInAscendingOrder(r => r.DisplayOrder);
    }

    #endregion

    #region ServiceTypes CRUD

    [Fact]
    public async Task ServiceTypes_GetAll_ReturnsSeededType()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(_client, "/api/admin/servicetypes", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Standard Cleaning", "seeded service type should be returned");
    }

    [Fact]
    public async Task ServiceTypes_Create_PersistsAllFields()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/servicetypes", token,
            new
            {
                Name = "Deep Cleaning",
                Description = "Thorough cleaning service",
                Price = 150.00,
                PricePerBedroom = 25.00,
                PricePerBathroom = 30.00,
                EstimatedMinutes = 120,
                MinutesPerBedroom = 30,
                MinutesPerBathroom = 25,
                DisplayOrder = 2
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ServiceTypeResponse>(JsonOpts);
        created!.Name.Should().Be("Deep Cleaning");
        created.Price.Should().Be(150.00m);
        created.PricePerBedroom.Should().Be(25.00m);
        created.PricePerBathroom.Should().Be(30.00m);
        created.EstimatedMinutes.Should().Be(120);
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ServiceTypes_Delete_SoftDeletes_NoLongerInList()
    {
        var token = await GetAdminTokenAsync();

        var createResp = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/servicetypes", token,
            new { Name = "Temp Service", Price = 50.00m });
        var created = await createResp.Content.ReadFromJsonAsync<IdResponse>();

        var delResp = await TestAuthHelper.DeleteAuthenticatedAsync(
            _client, $"/api/admin/servicetypes/{created!.Id}", token);
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Should not appear in GetById
        var getResp = await TestAuthHelper.GetAuthenticatedAsync(
            _client, $"/api/admin/servicetypes/{created.Id}", token);
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "soft-deleted service types should not be found");
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task AdminEndpoints_WithCustomerToken_ReturnsForbidden()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = TestDataSeeder.CustomerEmail,
            Password = TestDataSeeder.TestPassword
        });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces", auth!.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customers should not have access to admin endpoints");
    }

    [Fact]
    public async Task AdminEndpoints_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/cleaningplaces");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated requests should be rejected");
    }

    #endregion

    #region Not Found

    [Fact]
    public async Task CleaningPlaces_GetNonExistent_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces/9999", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CleaningPlaces_UpdateNonExistent_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PutAuthenticatedAsync(
            _client, "/api/admin/cleaningplaces/9999", token,
            new { Name = "Ghost", Description = "Does not exist", IsActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ServiceTypes_GetNonExistent_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/servicetypes/9999", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}

public record IdResponse(int Id);
public record CleaningPlaceResponse(int Id, string Name, string? Description, bool IsActive);
public record RoomResponse(int Id, string Name, string? Description, int BaseMinutes, decimal BasePrice, int DisplayOrder, bool IsActive);
public record ServiceTypeResponse(int Id, string Name, string? Description, decimal Price, decimal PricePerBedroom, decimal PricePerBathroom, int EstimatedMinutes, bool IsActive);
