using System.Net;
using System.Text.Json;
using LittleEcommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LittleEcommerce.IntegrationTests.Foundation;

[Collection(IntegrationCollection.Name)]
public class FoundationEndpointsTests(IntegrationFixture fixture)
{
    private readonly ApiFactory _factory = fixture.Factory;
    private readonly HttpClient _client = fixture.Factory.CreateClient();

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
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Documentation_ShouldBeHiddenInProduction()
    {
        using var production = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = production.CreateClient();

        var swagger = await client.GetAsync("/swagger/index.html");
        var document = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, swagger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, document.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_ShouldRequireBearerOnlyOnProtectedRoutes()
    {
        var body = await ReadJsonAsync(await _client.GetAsync("/openapi/v1.json"));

        var scheme = body.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

        var paths = body.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/v1/users/me").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/v1/auth/login").GetProperty("post").TryGetProperty("security", out _));
    }

    [Fact]
    public async Task HealthReady_ShouldBeHealthyWithAllDependencies()
    {
        var response = await _client.GetAsync("/health/ready");

        var body = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Equal(3, body.GetProperty("checks").GetArrayLength());
    }
}
