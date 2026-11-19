namespace LittleEcommerce.Source.Application.Exceptions;

public class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}
