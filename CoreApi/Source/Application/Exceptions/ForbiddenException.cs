namespace LittleEcommerce.Source.Application.Exceptions;

public class ForbiddenException(string message = "Você não tem permissão para esta ação") : AppException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}
