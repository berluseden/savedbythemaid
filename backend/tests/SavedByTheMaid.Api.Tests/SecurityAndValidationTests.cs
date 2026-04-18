using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SavedByTheMaid.Api.Tests.Helpers;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Tests for security fixes: authorization on write endpoints and input validation.
/// Verifies that unauthenticated/unauthorized users cannot mutate data,
/// and that invalid input is rejected with proper error responses.
/// </summary>
public class SecurityAndValidationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly HttpClient _client;
    private string? _adminToken;
    private string? _customerToken;

    public SecurityAndValidationTests(SeededWebApplicationFactory factory)
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

    private async Task<string> GetCustomerTokenAsync()
    {
        if (_customerToken != null) return _customerToken;
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = TestDataSeeder.CustomerEmail,
            Password = TestDataSeeder.TestPassword
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        _customerToken = auth!.AccessToken;
        return _customerToken;
    }

    #region EmployeesController Authorization

    [Fact]
    public async Task Employees_GetAll_IsPublic()
    {
        // GET is public (booking flow needs employee list)
        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/employees should be publicly accessible for the booking flow");
    }

    [Fact]
    public async Task Employees_Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/employees", new
        {
            FirstName = "Hacker",
            LastName = "McHackface"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "creating employees without authentication should be rejected");
    }

    [Fact]
    public async Task Employees_Create_WithCustomerToken_ReturnsForbidden()
    {
        var token = await GetCustomerTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/employees", token,
            new { FirstName = "Sneaky", LastName = "Customer" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customers should not be able to create employees");
    }

    [Fact]
    public async Task Employees_Create_WithAdminToken_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/employees", token,
            new { FirstName = "New", LastName = "Employee" });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "admins should be able to create employees");
    }

    [Fact]
    public async Task Employees_AddSchedule_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/employees/1/schedule", new
        {
            DayOfWeek = 6, // Saturday
            StartTime = "08:00:00",
            EndTime = "14:00:00"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "adding schedules without authentication should be rejected");
    }

    #endregion

    #region ServiceAreasController Authorization

    [Fact]
    public async Task ServiceAreas_GetAll_IsPublic()
    {
        var response = await _client.GetAsync("/api/serviceareas");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/serviceareas should be publicly accessible for coverage checks");
    }

    [Fact]
    public async Task ServiceAreas_Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/serviceareas", new
        {
            Name = "Unauthorized Area"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "creating service areas without authentication should be rejected");
    }

    [Fact]
    public async Task ServiceAreas_Create_WithCustomerToken_ReturnsForbidden()
    {
        var token = await GetCustomerTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/serviceareas", token,
            new { Name = "Sneaky Area" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customers should not be able to create service areas");
    }

    [Fact]
    public async Task ServiceAreas_Update_WithCustomerToken_ReturnsForbidden()
    {
        var token = await GetCustomerTokenAsync();
        var response = await TestAuthHelper.PutAuthenticatedAsync(
            _client, "/api/serviceareas/1", token,
            new { Name = "Hijacked", Description = "Nope", IsActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customers should not be able to update service areas");
    }

    [Fact]
    public async Task ServiceAreas_Delete_WithCustomerToken_ReturnsForbidden()
    {
        var token = await GetCustomerTokenAsync();
        var response = await TestAuthHelper.DeleteAuthenticatedAsync(
            _client, "/api/serviceareas/1", token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customers should not be able to delete service areas");
    }

    [Fact]
    public async Task ServiceAreas_AddZipCode_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/serviceareas/1/zipcodes", new
        {
            ZipCode = "99999"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "adding zip codes without authentication should be rejected");
    }

    #endregion

    #region Schedule EndTime > StartTime Validation

    [Fact]
    public async Task Employees_AddSchedule_EndTimeBeforeStartTime_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/employees/1/schedule", token,
            new
            {
                DayOfWeek = 6, // Saturday
                StartTime = "14:00:00",
                EndTime = "08:00:00" // End before start - invalid
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "schedule with EndTime before StartTime should be rejected");
    }

    [Fact]
    public async Task Employees_AddSchedule_EndTimeEqualsStartTime_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/employees/1/schedule", token,
            new
            {
                DayOfWeek = 6,
                StartTime = "10:00:00",
                EndTime = "10:00:00" // Same as start - invalid
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "schedule with EndTime equal to StartTime should be rejected");
    }

    [Fact]
    public async Task Employees_AddSchedule_ValidTimes_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/employees/1/schedule", token,
            new
            {
                DayOfWeek = 6,
                StartTime = "08:00:00",
                EndTime = "14:00:00"
            });

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "valid schedule should be accepted");
    }

    [Fact]
    public async Task AdminEmployees_AddSchedule_EndTimeBeforeStartTime_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/admin/employees/1/schedules", token,
            new
            {
                DayOfWeek = 0, // Sunday
                StartTime = "18:00:00",
                EndTime = "08:00:00" // End before start
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "admin schedule endpoint should also reject EndTime before StartTime");
    }

    #endregion

    #region UsersController uses Policy (not Roles)

    [Fact]
    public async Task AdminUsers_WithCustomerToken_ReturnsForbidden()
    {
        var token = await GetCustomerTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/users", token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customers should not access admin user management");
    }

    [Fact]
    public async Task AdminUsers_WithAdminToken_PassesAuthorization()
    {
        var token = await GetAdminTokenAsync();
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/admin/users", token);

        // Verify admin passes the Policy authorization (not blocked by 401/403)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "admin token should pass authentication");
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "admin should pass the AdminOnly policy authorization");
    }

    #endregion
}
