using System.Diagnostics.CodeAnalysis;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Common.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LittleEcommerce.Source.Infrastructure.Data;

public static class DatabaseExceptionTranslator
{
    public static bool TryTranslate(Exception exception, [NotNullWhen(true)] out AppException? translated)
    {
        translated = exception switch
        {
            DbUpdateConcurrencyException => new ConflictException(
                "O registro foi alterado por outra operação. Recarregue e tente de novo.",
                ErrorCodes.ConcurrencyConflict,
                exception),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                new ConflictException("Já existe um registro com esses dados", ErrorCodes.RecordAlreadyExists, exception),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                new ConflictException("O registro está vinculado a outro e não pode ser alterado", ErrorCodes.RecordInUse, exception),
            _ => null
        };

        return translated is not null;
    }
}
