using LittleEcommerce.Source.Shared.DTOs;

namespace LittleEcommerce.Source.Application.Exceptions;

public class ValidationException(IReadOnlyDictionary<string, string[]> errors, string message = "Dados inválidos")
    : AppException(message)
{
    public ValidationException(string field, string error) : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string Code => APIErrors.ValidationCode;

    public override IReadOnlyDictionary<string, string[]> Errors => errors;
}
