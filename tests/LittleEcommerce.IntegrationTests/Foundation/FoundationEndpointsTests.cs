using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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
    public async Task OpenApiDocument_ShouldDescribeApiAndErrorEnvelope()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        var body = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("LittleEcommerce API", body.GetProperty("info").GetProperty("title").GetString());
        Assert.True(body.GetProperty("components").GetProperty("schemas").TryGetProperty("APIErrorResponse", out _));

        var operation = body.GetProperty("paths").GetProperty("/api/v1/system/version").GetProperty("get");
        Assert.Equal("Versão da API", operation.GetProperty("summary").GetString());
        Assert.True(operation.GetProperty("responses").TryGetProperty("500", out _));
    }

    [Fact]
    public async Task SwaggerUi_ShouldBeServedInDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Root_ShouldRedirectToSwagger()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Documentation_ShouldBeHiddenInProduction()
    {
        using var production = factory.WithWebHostBuilder(b => b
            .UseEnvironment("Production")
            .UseSetting("ConnectionStrings:PostgreSQL", "Host=localhost")
            .UseSetting("ConnectionStrings:Redis", "localhost:6379")
            .UseSetting("ConnectionStrings:Mongo", "mongodb://localhost:27017"));
        using var client = production.CreateClient();

        var swagger = await client.GetAsync("/swagger/index.html");
        var document = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, swagger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, document.StatusCode);
    }
}
