namespace LittleEcommerce.Source.Application.Interfaces.Caching;

public interface ICatalogCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory);

    Task InvalidateAsync();
}
