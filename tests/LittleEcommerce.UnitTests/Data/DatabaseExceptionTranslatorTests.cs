using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LittleEcommerce.UnitTests.Data;

public class DatabaseExceptionTranslatorTests
{
    private static DbUpdateException Postgres(string sqlState) =>
        new("falha", new PostgresException("erro", "ERROR", "ERROR", sqlState));

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, ErrorCodes.RecordAlreadyExists)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, ErrorCodes.RecordInUse)]
    public void TryTranslate_ShouldMapConstraintViolationsToConflict(string sqlState, string expectedCode)
    {
        var translated = DatabaseExceptionTranslator.TryTranslate(Postgres(sqlState), out var result);

        Assert.True(translated);
        var conflict = Assert.IsType<ConflictException>(result);
        Assert.Equal(expectedCode, conflict.Code);
        Assert.IsType<DbUpdateException>(conflict.InnerException);
    }

    [Fact]
    public void TryTranslate_ShouldMapConcurrencyConflict()
    {
        var translated = DatabaseExceptionTranslator.TryTranslate(new DbUpdateConcurrencyException("concorrência"), out var result);

        Assert.True(translated);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, result!.Code);
    }

    [Fact]
    public void TryTranslate_ShouldLeaveOtherErrorsAlone()
    {
        Assert.False(DatabaseExceptionTranslator.TryTranslate(Postgres(PostgresErrorCodes.DeadlockDetected), out _));
        Assert.False(DatabaseExceptionTranslator.TryTranslate(new InvalidOperationException(), out _));
    }
}
