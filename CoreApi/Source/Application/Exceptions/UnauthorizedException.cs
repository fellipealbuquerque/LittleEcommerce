namespace LittleEcommerce.Source.Application.Exceptions;

public class UnauthorizedException(string message = "Não autenticado", string? code = null, Exception? innerException = null)
    : AppException(message, code, innerException)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}
