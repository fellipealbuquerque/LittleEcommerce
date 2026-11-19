using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Infrastructure.Logging;
using LittleEcommerce.Source.Shared.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog.Events;
using Serilog.Parsing;

namespace LittleEcommerce.IntegrationTests.Logging;

[Collection(MongoCollection.Name)]
public class MongoLogSinkTests(MongoContainerFixture mongo)
{
    private static readonly MessageTemplateParser Parser = new();

    private static LogEvent Event(string text) =>
        new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, Parser.Parse(text), []);

    private static async Task<List<BsonDocument>> IndexesAsync(IMongoCollection<BsonDocument> collection) =>
        await (await collection.Indexes.ListAsync()).ToListAsync();

    private static long TtlSeconds(IEnumerable<BsonDocument> indexes) =>
        indexes.Single(i => i["name"].AsString == MongoLogSink.TtlIndexName)["expireAfterSeconds"].ToInt64();

    [Fact]
    public async Task EmitBatchAsync_ShouldInsertDocumentsAndCreateIndexes()
    {
        var database = mongo.NewDatabase();
        var sink = new MongoLogSink(database, new MongoLoggingOptions { Collection = "logs", RetentionDays = 7 });

        await sink.EmitBatchAsync([Event("primeiro"), Event("segundo")]);

        var collection = database.GetCollection<BsonDocument>("logs");
        var indexes = await IndexesAsync(collection);

        Assert.Equal(2, await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Contains(indexes, i => i["name"].AsString == "level_timestamp");
        Assert.Contains(indexes, i => i["name"].AsString == "correlation_id");
        Assert.Equal(7 * 86_400, TtlSeconds(indexes));
    }

    [Fact]
    public async Task EmitBatchAsync_ShouldUpdateTtl_WhenRetentionChanges()
    {
        var database = mongo.NewDatabase();
        await new MongoLogSink(database, new MongoLoggingOptions { RetentionDays = 7 }).EmitBatchAsync([Event("antes")]);

        await new MongoLogSink(database, new MongoLoggingOptions { RetentionDays = 14 }).EmitBatchAsync([Event("depois")]);

        var collection = database.GetCollection<BsonDocument>("logs");
        Assert.Equal(14 * 86_400, TtlSeconds(await IndexesAsync(collection)));
        Assert.Equal(2, await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }
}
