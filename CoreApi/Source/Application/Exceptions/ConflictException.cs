namespace LittleEcommerce.Source.Application.Exceptions;

public class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}
