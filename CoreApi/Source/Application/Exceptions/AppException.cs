using LittleEcommerce.Source.Common.Errors;

namespace LittleEcommerce.Source.Application.Exceptions;

public abstract class AppException(string message, string? code = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public abstract int StatusCode { get; }

    public virtual string Code => code ?? HttpErrorCatalog.Describe(StatusCode).Code;

    public virtual IReadOnlyDictionary<string, string[]>? Errors => null;
}
