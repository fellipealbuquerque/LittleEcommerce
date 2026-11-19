using LittleEcommerce.Source.Shared.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace LittleEcommerce.Source.Infrastructure.Logging;

public sealed class MongoLogSink(IMongoDatabase database, MongoLoggingOptions options) : IBatchedLogEventSink
{
    public const string TtlIndexName = "ttl_timestamp";

    private const int IndexOptionsConflict = 85;
    private const int IndexKeySpecsConflict = 86;

    private readonly IMongoCollection<BsonDocument> _collection = database.GetCollection<BsonDocument>(options.Collection);
    private volatile bool _indexesReady;

    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        if (!_indexesReady)
            await EnsureIndexesAsync();

        var documents = batch.Select(MongoLogDocumentFactory.Create).ToList();
        await _collection.InsertManyAsync(documents, new InsertManyOptions { IsOrdered = false });
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    private async Task EnsureIndexesAsync()
    {
        try
        {
            await EnsureTtlIndexAsync();

            var keys = Builders<BsonDocument>.IndexKeys;

            await _collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending("level").Descending("timestamp"),
                    new CreateIndexOptions { Name = "level_timestamp" }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending("correlationId"),
                    new CreateIndexOptions { Name = "correlation_id", Sparse = true }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending("userId").Descending("timestamp"),
                    new CreateIndexOptions { Name = "user_timestamp", Sparse = true })
            ]);

            _indexesReady = true;
        }
        catch (MongoCommandException ex)
        {
            SelfLog.WriteLine("MongoLogSink: índices não criados em {0}: {1}", options.Collection, ex.Message);
            _indexesReady = true;
        }
    }

    private async Task EnsureTtlIndexAsync()
    {
        var expireAfter = TimeSpan.FromDays(Math.Max(1, options.RetentionDays));

        try
        {
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("timestamp"),
                new CreateIndexOptions { Name = TtlIndexName, ExpireAfter = expireAfter }));
        }
        catch (MongoCommandException ex) when (ex.Code is IndexOptionsConflict or IndexKeySpecsConflict)
        {
            await database.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                ["collMod"] = options.Collection,
                ["index"] = new BsonDocument
                {
                    ["name"] = TtlIndexName,
                    ["expireAfterSeconds"] = (long)expireAfter.TotalSeconds
                }
            });
        }
    }
}
