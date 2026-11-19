namespace LittleEcommerce.Source.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);

    Task ExecuteAsync(Func<Task> action, CancellationToken ct = default);

    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
