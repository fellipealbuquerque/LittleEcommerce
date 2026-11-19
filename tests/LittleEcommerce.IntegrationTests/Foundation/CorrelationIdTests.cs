using System.Net;
using System.Text.Json;
using LittleEcommerce.Source.Middlewares;

namespace LittleEcommerce.IntegrationTests.Foundation;

public class CorrelationIdTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string HeaderOf(HttpResponseMessage response) =>
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();

    [Fact]
    public async Task Response_ShouldGenerateCorrelationId_WhenRequestHasNone()
    {
        var response = await _client.GetAsync("/api/v1/system/version");

        Assert.Equal(32, HeaderOf(response).Length);
    }

    [Fact]
    public async Task Response_ShouldEchoIncomingCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/version");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "pedido-777");

        var response = await _client.SendAsync(request);

        Assert.Equal("pedido-777", HeaderOf(response));
    }

    [Fact]
    public async Task ErrorEnvelope_ShouldCarryCorrelationIdAsTraceId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/nao-existe");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "erro-404");

        var response = await _client.SendAsync(request);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("erro-404", HeaderOf(response));
        Assert.Equal("erro-404", body.RootElement.GetProperty("traceId").GetString());
    }
}
