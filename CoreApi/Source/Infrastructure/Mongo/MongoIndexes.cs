using MongoDB.Bson;
using MongoDB.Driver;

namespace LittleEcommerce.Source.Infrastructure.Mongo;

public static class MongoIndexes
{
    public const string TtlIndexName = "ttl_timestamp";

    private const int IndexOptionsConflict = 85;
    private const int IndexKeySpecsConflict = 86;

    public static async Task EnsureTtlAsync<T>(IMongoDatabase database, IMongoCollection<T> collection, string field, int retentionDays)
    {
        var expireAfter = TimeSpan.FromDays(Math.Max(1, retentionDays));

        try
        {
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(
                Builders<T>.IndexKeys.Ascending(field),
                new CreateIndexOptions { Name = TtlIndexName, ExpireAfter = expireAfter }));
        }
        catch (MongoCommandException ex) when (ex.Code is IndexOptionsConflict or IndexKeySpecsConflict)
        {
            await database.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                ["collMod"] = collection.CollectionNamespace.CollectionName,
                ["index"] = new BsonDocument
                {
                    ["name"] = TtlIndexName,
                    ["expireAfterSeconds"] = (long)expireAfter.TotalSeconds
                }
            });
        }
    }
}
