namespace LittleEcommerce.Source.Application.Exceptions;

public class UnauthorizedException(string message = "Não autenticado") : AppException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}
