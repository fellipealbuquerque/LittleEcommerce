using System.Security.Claims;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

namespace LittleEcommerce.Source.Middlewares;

public class TokenRevocationMiddleware(RequestDelegate next, ILogger<TokenRevocationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITokenRevocationStore store)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var rejection = await CheckAsync(context, store);

        if (rejection is null)
        {
            await next(context);
            return;
        }

        if (!RequiresAuthorization(context))
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            await next(context);
            return;
        }

        logger.LogInformation("Token recusado | Code: {Code} | UserId: {UserId}",
            rejection.Value.Code,
            context.User.FindFirstValue(AppClaims.Subject));

        context.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
        await context.Response.WriteErrorAsync(APIErrors.Create(
            StatusCodes.Status401Unauthorized,
            rejection.Value.Message,
            context.TraceIdentifier,
            rejection.Value.Code));
    }

    private static async Task<(string Code, string Message)?> CheckAsync(HttpContext context, ITokenRevocationStore store)
    {
        var userId = ClaimsHelper.GetUserIdOrNull(context.User);
        var tokenId = context.User.FindFirstValue(AppClaims.TokenId);
        var sessionVersion = ClaimsHelper.GetSessionVersion(context.User);

        if (userId is null || tokenId is null || sessionVersion is null)
            return (ErrorCodes.TokenInvalid, "Token sem as informações de sessão esperadas");

        try
        {
            if (await store.IsRevokedAsync(tokenId))
                return (ErrorCodes.TokenRevoked, "Sessão encerrada. Entre novamente.");

            var current = await store.GetSessionVersionAsync(userId.Value, context.RequestAborted);

            return current == sessionVersion
                ? null
                : (ErrorCodes.SessionRevoked, "Sessão revogada. Entre novamente.");
        }
        catch (RedisException ex)
        {
            throw new ServiceUnavailableException(
                "Serviço de autenticação indisponível. Tente novamente em instantes.",
                ErrorCodes.AuthUnavailable,
                ex);
        }
    }

    private static bool RequiresAuthorization(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        return endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null
               && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;
    }
}
