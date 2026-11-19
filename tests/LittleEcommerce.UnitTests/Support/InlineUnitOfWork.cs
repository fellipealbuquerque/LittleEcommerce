using LittleEcommerce.Source.Application.Interfaces;

namespace LittleEcommerce.UnitTests.Support;

internal sealed class InlineUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        Commits++;
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken ct = default)
    {
        await action();
        Commits++;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var result = await action();
        Commits++;
        return result;
    }
}
