using System.Linq.Expressions;
using LittleEcommerce.Source.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        var softDeletable = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.BaseType is null && typeof(ISoftDeletable).IsAssignableFrom(t.ClrType))
            .ToList();

        foreach (var entityType in softDeletable)
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var notDeleted = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(notDeleted, parameter));
        }

        return modelBuilder;
    }
}
