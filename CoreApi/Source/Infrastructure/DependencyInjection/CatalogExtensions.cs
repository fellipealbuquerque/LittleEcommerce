using LittleEcommerce.Source.Application.Interfaces.Caching;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Infrastructure.Caching;
using LittleEcommerce.Source.Shared.Options;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class CatalogExtensions
{
    public static IServiceCollection AddCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CatalogCacheOptions>(configuration.GetSection(CatalogCacheOptions.Section));

        services.AddSingleton<ICatalogCache, RedisCatalogCache>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IProductAdminService, ProductAdminService>();
        services.AddScoped<ICategoryAdminService, CategoryAdminService>();

        return services;
    }
}
