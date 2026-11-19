using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace LittleEcommerce.IntegrationTests.Infrastructure;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private const int SmtpPort = 1025;
    private const int MailpitHttpPort = 8025;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8").Build();

    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:latest")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(MailpitHttpPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(MailpitHttpPort).ForPath("/api/v1/info")))
        .Build();

    public ApiFactory Factory { get; private set; } = null!;

    public HttpClient Mailpit { get; private set; } = null!;

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string RedisConnectionString => _redis.GetConnectionString();

    public string MongoConnectionString => _mongo.GetConnectionString();

    public string SmtpHost => _mailpit.Hostname;

    public int SmtpMappedPort => _mailpit.GetMappedPublicPort(SmtpPort);

    public IMongoDatabase NewMongoDatabase() =>
        new MongoClient(MongoConnectionString).GetDatabase($"test_{Guid.NewGuid():N}");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _mongo.StartAsync(), _mailpit.StartAsync());

        Mailpit = new HttpClient { BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(MailpitHttpPort)}") };
        Factory = new ApiFactory(this);
    }

    public async Task DisposeAsync()
    {
        Mailpit.Dispose();
        await Factory.DisposeAsync();
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _mongo.DisposeAsync().AsTask(),
            _mailpit.DisposeAsync().AsTask());
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    public const string Name = "integration";
}
