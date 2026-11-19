using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LittleEcommerce.IntegrationTests.Infrastructure;

public record Session(string AccessToken, string RefreshToken, string UserId, string Email);

public static class AuthApi
{
    public const string Password = "Senha12345";

    public static string NewEmail() => $"cliente-{Guid.NewGuid():N}@tests.local";

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    public static async Task<string?> ErrorCodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("code").GetString();

    public static Task<HttpResponseMessage> PostRegisterAsync(this HttpClient client, string email, string password = Password, string name = "Cliente Teste") =>
        client.PostAsJsonAsync("/api/v1/auth/register", new { name, email, password });

    public static Task<HttpResponseMessage> PostLoginAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    public static Task<HttpResponseMessage> PostRefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

    public static async Task<Session> RegisterAsync(this HttpClient client, string? email = null, string password = Password)
    {
        email ??= NewEmail();
        var response = await client.PostRegisterAsync(email, password);
        return await ToSessionAsync(response, email);
    }

    public static async Task<Session> LoginAsync(this HttpClient client, string email, string password = Password)
    {
        var response = await client.PostLoginAsync(email, password);
        return await ToSessionAsync(response, email);
    }

    public static async Task<Session> RefreshAsync(this HttpClient client, Session session)
    {
        var response = await client.PostRefreshAsync(session.RefreshToken);
        return await ToSessionAsync(response, session.Email);
    }

    public static Task<HttpResponseMessage> SendAuthorizedAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string accessToken) =>
        client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", accessToken);

    private static async Task<Session> ToSessionAsync(HttpResponseMessage response, string email)
    {
        var body = await response.ReadJsonAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Falha de autenticação {(int)response.StatusCode}: {body}");

        var data = body.GetProperty("data");

        return new Session(
            data.GetProperty("accessToken").GetString()!,
            data.GetProperty("refreshToken").GetString()!,
            data.GetProperty("user").GetProperty("id").GetString()!,
            email);
    }
}
