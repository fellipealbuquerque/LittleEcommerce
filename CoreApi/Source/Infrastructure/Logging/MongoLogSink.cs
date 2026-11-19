using LittleEcommerce.Source.Infrastructure.Mongo;
using LittleEcommerce.Source.Shared.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace LittleEcommerce.Source.Infrastructure.Logging;

public sealed class MongoLogSink(IMongoDatabase database, MongoLoggingOptions options) : IBatchedLogEventSink
{
    public const string TtlIndexName = MongoIndexes.TtlIndexName;

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
            await MongoIndexes.EnsureTtlAsync(database, _collection, "timestamp", options.RetentionDays);

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
}
