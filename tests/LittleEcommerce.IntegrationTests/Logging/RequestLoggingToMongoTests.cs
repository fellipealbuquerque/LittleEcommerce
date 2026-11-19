using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Middlewares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LittleEcommerce.IntegrationTests.Logging;

[Collection(IntegrationCollection.Name)]
public sealed class RequestLoggingToMongoTests : IDisposable
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(20);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly IMongoCollection<BsonDocument> _logs;

    public RequestLoggingToMongoTests(IntegrationFixture fixture)
    {
        var databaseName = $"test_{Guid.NewGuid():N}";

        _factory = fixture.Factory.WithWebHostBuilder(b => b
            .UseSetting("Mongo:Database", databaseName)
            .UseSetting("MongoLogging:Enabled", "true")
            .UseSetting("MongoLogging:FlushIntervalSeconds", "1"));

        _logs = new MongoClient(fixture.MongoConnectionString).GetDatabase(databaseName).GetCollection<BsonDocument>("logs");
    }

    public void Dispose() => _factory.Dispose();

    private async Task<string> SendAsync(string path)
    {
        var correlationId = $"e2e-{Guid.NewGuid():N}";
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        await client.SendAsync(request);

        return correlationId;
    }

    private async Task<BsonDocument> WaitForRequestLogAsync(string correlationId)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("correlationId", correlationId),
            Builders<BsonDocument>.Filter.Regex("template", new BsonRegularExpression("^HTTP ")));

        var deadline = DateTime.UtcNow + WaitLimit;

        while (DateTime.UtcNow < deadline)
        {
            var document = await _logs.Find(filter).FirstOrDefaultAsync();
            if (document is not null)
                return document;

            await Task.Delay(250);
        }

        throw new TimeoutException($"Log da requisição {correlationId} não chegou ao Mongo em {WaitLimit.TotalSeconds}s");
    }

    [Fact]
    public async Task SuccessfulRequest_ShouldBeLoggedWithCorrelationAndMaskedQuery()
    {
        var correlationId = await SendAsync("/api/v1/system/version?token=segredo-123&page=1");

        var log = await WaitForRequestLogAsync(correlationId);

        Assert.Equal("Information", log["level"].AsString);
        Assert.Equal("/api/v1/system/version", log["requestPath"].AsString);
        Assert.Equal(200, log["statusCode"].AsInt32);
        Assert.Equal("GET", log["properties"]["RequestMethod"].AsString);
        Assert.Equal("LittleEcommerce", log["properties"]["Application"].AsString);
        Assert.DoesNotContain("segredo-123", log.ToJson());
    }

    [Fact]
    public async Task NotFoundRequest_ShouldBeLoggedAsWarning()
    {
        var correlationId = await SendAsync("/api/v1/nao-existe");

        var log = await WaitForRequestLogAsync(correlationId);

        Assert.Equal("Warning", log["level"].AsString);
        Assert.Equal(404, log["statusCode"].AsInt32);
    }
}
