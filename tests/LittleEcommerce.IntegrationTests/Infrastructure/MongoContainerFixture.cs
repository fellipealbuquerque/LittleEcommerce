using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace LittleEcommerce.IntegrationTests.Infrastructure;

public sealed class MongoContainerFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8").Build();

    public string ConnectionString => _container.GetConnectionString();

    public IMongoDatabase NewDatabase() =>
        new MongoClient(ConnectionString).GetDatabase($"test_{Guid.NewGuid():N}");

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class MongoCollection : ICollectionFixture<MongoContainerFixture>
{
    public const string Name = "mongo";
}
