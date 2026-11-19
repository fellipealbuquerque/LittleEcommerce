namespace LittleEcommerce.Source.Application.Exceptions;

public class DomainException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
}
