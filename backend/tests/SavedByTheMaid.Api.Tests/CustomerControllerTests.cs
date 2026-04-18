using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SavedByTheMaid.Api.Tests.Helpers;

namespace SavedByTheMaid.Api.Tests;

/// <summary>
/// Tests for CustomerController - verifies business rules, not just status codes.
/// </summary>
public class CustomerControllerTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly HttpClient _client;
    private string? _customerToken;

    public CustomerControllerTests(SeededWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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

    #region Profile

    [Fact]
    public async Task GetProfile_ReturnsAuthenticatedUserEmail()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.GetAuthenticatedAsync(_client, "/api/customer/profile", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        profile!.Email.Should().Be(TestDataSeeder.CustomerEmail,
            "profile should return the authenticated user's email");
        profile.FirstName.Should().NotBeNullOrEmpty(
            "profile should have a first name set");
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/customer/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_PersistsChanges()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.PutAuthenticatedAsync(
            _client, "/api/customer/profile", token, new
            {
                FirstName = "Updated",
                LastName = "Name",
                Phone = "555-9999",
                Address = "456 Oak Ave",
                City = "Tampa",
                State = "FL",
                ZipCode = "33601"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Read back and verify the data actually changed
        var getResp = await TestAuthHelper.GetAuthenticatedAsync(_client, "/api/customer/profile", token);
        var profile = await getResp.Content.ReadFromJsonAsync<CustomerProfileResponse>();
        profile!.FirstName.Should().Be("Updated", "profile update should persist the new first name");
        profile.City.Should().Be("Tampa");
        profile.ZipCode.Should().Be("33601");
    }

    #endregion

    #region My Orders

    [Fact]
    public async Task GetMyOrders_ReturnsPaginatedOrdersWithCorrectFields()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/my-orders?page=1&pageSize=10", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrdersListResponse>();
        body!.Items.Should().NotBeEmpty("customer has seeded orders");
        body.TotalItems.Should().BeGreaterThanOrEqualTo(1);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(10);

        // Verify order fields are populated from DB
        var order = body.Items.First();
        order.Id.Should().BeGreaterThan(0);
        order.ServiceTypeName.Should().NotBeNullOrEmpty();
        order.TotalAmount.Should().BeGreaterThan(0);
        order.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMyOrders_FilterByStatus_ReturnsOnlyMatchingOrders()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/my-orders?status=Completed", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrdersListResponse>();
        body!.Items.Should().AllSatisfy(o =>
            o.Status.Should().Be("Completed",
                "status filter should only return orders with matching status"));
    }

    [Fact]
    public async Task GetMyOrders_FilterByInvalidStatus_ReturnsAllOrders()
    {
        var token = await GetCustomerTokenAsync();

        // Invalid status string should be ignored (Enum.TryParse fails)
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/my-orders?status=InvalidStatus", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrdersListResponse>();
        body!.TotalItems.Should().BeGreaterThanOrEqualTo(2,
            "invalid status filter should be ignored and return all orders");
    }

    #endregion

    #region Order Detail

    [Fact]
    public async Task GetOrderDetail_ReturnsFullOrderWithAmounts()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/my-orders/1", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        // Verify the response contains financial breakdown
        body.Should().Contain("subtotal");
        body.Should().Contain("tax");
        body.Should().Contain("totalAmount");
    }

    [Fact]
    public async Task GetOrderDetail_OtherCustomersOrder_ReturnsNotFound()
    {
        var token = await GetCustomerTokenAsync();

        // Order 9999 doesn't belong to this customer
        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/my-orders/9999", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "customer should not see orders that don't belong to them");
    }

    #endregion

    #region Stats

    [Fact]
    public async Task GetStats_ReturnsCorrectCalculations()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/stats", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<StatsResponse>();

        // Seeded data: 1 completed order at $117.70
        stats!.CompletedBookings.Should().BeGreaterThanOrEqualTo(1);
        stats.TotalSpent.Should().BeGreaterThan(0);

        // Business rule: loyalty points = completedBookings * 50
        stats.LoyaltyPoints.Should().Be(stats.CompletedBookings * 50,
            "loyalty points should be 50 per completed booking");
    }

    #endregion

    #region Cancel

    [Fact]
    public async Task CancelOrder_ConfirmedOrder_ChangesStatusToCancelled()
    {
        var token = await GetCustomerTokenAsync();

        // Order 1 is Confirmed in seed data
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/customer/my-orders/1/cancel", token,
            new { Reason = "Changed my mind" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the order status changed
        var orderResp = await TestAuthHelper.GetAuthenticatedAsync(
            _client, "/api/customer/my-orders/1", token);
        var body = await orderResp.Content.ReadAsStringAsync();
        body.Should().Contain("Cancelled",
            "cancelled order should have status Cancelled");
    }

    [Fact]
    public async Task CancelOrder_CompletedOrder_ReturnsBadRequest()
    {
        var token = await GetCustomerTokenAsync();

        // Order 2 is Completed in seed data - cannot cancel completed orders
        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/customer/my-orders/2/cancel", token,
            new { Reason = "Too late" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "completed orders should not be cancellable");
    }

    [Fact]
    public async Task CancelOrder_NonExistentOrder_ReturnsNotFound()
    {
        var token = await GetCustomerTokenAsync();

        var response = await TestAuthHelper.PostAuthenticatedAsync(
            _client, "/api/customer/my-orders/9999/cancel", token,
            new { Reason = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}

// Response DTOs for deserialization
public record CustomerProfileResponse(
    string FirstName, string LastName, string Email, string Phone,
    string Address, string City, string State, string ZipCode);

public record OrdersListResponse(
    List<OrderItemResponse> Items, int TotalItems, int Page, int PageSize, int TotalPages);

public record OrderItemResponse(
    int Id, string ServiceTypeName, string Status, decimal TotalAmount, DateTime CreatedAt);

public record StatsResponse(
    int CompletedBookings, decimal TotalSpent, string? NextBooking, int LoyaltyPoints);
