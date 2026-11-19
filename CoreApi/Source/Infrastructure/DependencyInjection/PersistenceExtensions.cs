using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Data;
using LittleEcommerce.Source.Data.Repositories.Implementations;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Data.Seeders;
using LittleEcommerce.Source.Infrastructure.Auditing;
using LittleEcommerce.Source.Infrastructure.Data;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TimestampsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) => options
            .UseLittleEcommerceDatabase(sp.GetRequiredService<NpgsqlDataSource>())
            .AddInterceptors(
                sp.GetRequiredService<TimestampsInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditTransactionInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }

    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateOnStartup)
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count > 0)
        {
            app.Logger.LogInformation("Aplicando {Count} migrations: {Migrations}", pending.Count, string.Join(", ", pending));
            await context.Database.MigrateAsync();
        }

        await scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>().SeedAsync();
    }
}
