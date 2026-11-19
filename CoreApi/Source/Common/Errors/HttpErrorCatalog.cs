using Microsoft.AspNetCore.WebUtilities;

namespace LittleEcommerce.Source.Common.Errors;

public static class HttpErrorCatalog
{
    private static readonly Dictionary<int, (string Code, string Title)> Entries = new()
    {
        [StatusCodes.Status400BadRequest] = ("BAD_REQUEST", "Requisição inválida"),
        [StatusCodes.Status401Unauthorized] = ("UNAUTHORIZED", "Não autenticado"),
        [StatusCodes.Status403Forbidden] = ("FORBIDDEN", "Acesso negado"),
        [StatusCodes.Status404NotFound] = ("NOT_FOUND", "Recurso não encontrado"),
        [StatusCodes.Status405MethodNotAllowed] = ("METHOD_NOT_ALLOWED", "Método não permitido"),
        [StatusCodes.Status409Conflict] = ("CONFLICT", "Conflito com o estado atual"),
        [StatusCodes.Status413PayloadTooLarge] = ("PAYLOAD_TOO_LARGE", "Conteúdo muito grande"),
        [StatusCodes.Status415UnsupportedMediaType] = ("UNSUPPORTED_MEDIA_TYPE", "Tipo de conteúdo não suportado"),
        [StatusCodes.Status422UnprocessableEntity] = ("BUSINESS_RULE", "Regra de negócio violada"),
        [StatusCodes.Status429TooManyRequests] = ("TOO_MANY_REQUESTS", "Muitas requisições"),
        [StatusCodes.Status500InternalServerError] = ("INTERNAL_ERROR", "Erro interno inesperado"),
        [StatusCodes.Status503ServiceUnavailable] = ("SERVICE_UNAVAILABLE", "Serviço indisponível")
    };

    public static (string Code, string Title) Describe(int status)
    {
        if (Entries.TryGetValue(status, out var entry))
            return entry;

        var phrase = ReasonPhrases.GetReasonPhrase(status);
        return ($"HTTP_{status}", string.IsNullOrEmpty(phrase) ? "Erro" : phrase);
    }
}
