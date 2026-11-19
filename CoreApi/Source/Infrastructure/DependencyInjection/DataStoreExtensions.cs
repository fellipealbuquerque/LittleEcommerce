using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Npgsql;
using StackExchange.Redis;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class DataStoreExtensions
{
    public static IServiceCollection AddDataStores(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.RequiredConnectionString(ConnectionStringNames.PostgreSQL);
        var redis = configuration.RequiredConnectionString(ConnectionStringNames.Redis);
        var mongo = configuration.RequiredConnectionString(ConnectionStringNames.Mongo);

        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.Section));

        services.AddSingleton(_ => NpgsqlDataSource.Create(postgres));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redis, ignoreUnknown: true);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 1;
            options.ConnectTimeout = 2_000;
            options.SyncTimeout = 3_000;
            options.ReconnectRetryPolicy = new ExponentialRetry(5_000);
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<IMongoClient>(_ =>
        {
            var settings = MongoClientSettings.FromConnectionString(mongo);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(settings);
        });

        services.AddSingleton(sp =>
        {
            var database = sp.GetRequiredService<IOptions<MongoOptions>>().Value.Database;
            return sp.GetRequiredService<IMongoClient>().GetDatabase(database);
        });

        return services;
    }
}
