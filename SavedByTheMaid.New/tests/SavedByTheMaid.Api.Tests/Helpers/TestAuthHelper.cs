using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SavedByTheMaid.Api.Tests.Helpers;

/// <summary>
/// Helper for creating authenticated users in integration tests.
/// </summary>
public static class TestAuthHelper
{
    public static async Task<(string Token, string UserId)> RegisterAndGetTokenAsync(
        HttpClient client, string? email = null, string password = "TestPassword123!")
    {
        email ??= $"test_{Guid.NewGuid()}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            Name = "Test User"
        });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.AccessToken, auth.User.Id);
    }

    public static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpClient client, HttpMethod method, string url, string token, object? body = null)
    {
        var request = CreateAuthenticatedRequest(method, url, token);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> GetAuthenticatedAsync(
        HttpClient client, string url, string token)
        => await SendAuthenticatedAsync(client, HttpMethod.Get, url, token);

    public static async Task<HttpResponseMessage> PostAuthenticatedAsync(
        HttpClient client, string url, string token, object? body = null)
        => await SendAuthenticatedAsync(client, HttpMethod.Post, url, token, body);

    public static async Task<HttpResponseMessage> PutAuthenticatedAsync(
        HttpClient client, string url, string token, object body)
        => await SendAuthenticatedAsync(client, HttpMethod.Put, url, token, body);

    public static async Task<HttpResponseMessage> DeleteAuthenticatedAsync(
        HttpClient client, string url, string token)
        => await SendAuthenticatedAsync(client, HttpMethod.Delete, url, token);
}
