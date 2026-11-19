namespace LittleEcommerce.Source.Application.Exceptions;

public class ForbiddenException(string message = "Você não tem permissão para esta ação", string? code = null, Exception? innerException = null)
    : AppException(message, code, innerException)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}
