using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LittleEcommerce.IntegrationTests.Foundation;

public class FoundationEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task HealthLive_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        var body = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task SystemVersion_ShouldReturnSuccessEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/system/version");

        var body = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(200, body.GetProperty("status").GetInt32());
        Assert.Equal("LittleEcommerce", body.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal("Development", body.GetProperty("data").GetProperty("environment").GetString());
    }

    [Fact]
    public async Task UnknownRoute_ShouldReturnNotFoundEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/rota-que-nao-existe");

        var body = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task OpenApiDocument_ShouldBeExposedInDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
