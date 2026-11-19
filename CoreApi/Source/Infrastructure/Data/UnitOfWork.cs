using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Data;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Infrastructure.Data;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (DatabaseExceptionTranslator.TryTranslate(ex, out var translated))
        {
            throw translated;
        }
    }

    public Task ExecuteAsync(Func<Task> action, CancellationToken ct = default) =>
        ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, ct);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async token =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(token);

                try
                {
                    var result = await action();
                    await context.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    context.ChangeTracker.Clear();
                    throw;
                }
            }, ct);
        }
        catch (Exception ex) when (DatabaseExceptionTranslator.TryTranslate(ex, out var translated))
        {
            throw translated;
        }
    }
}
