namespace LittleEcommerce.Source.Application.Exceptions;

public class BadRequestException(string message, string? code = null, Exception? innerException = null)
    : AppException(message, code, innerException)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}
