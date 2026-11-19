using LittleEcommerce.Source.Common.Errors;

namespace LittleEcommerce.Source.Application.Exceptions;

public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }

    public virtual string Code => HttpErrorCatalog.Describe(StatusCode).Code;

    public virtual IReadOnlyDictionary<string, string[]>? Errors => null;
}
