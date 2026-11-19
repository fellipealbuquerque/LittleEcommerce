using System.Security.Claims;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Common.Constants;

namespace LittleEcommerce.Source.Shared.Helpers;

public static class ClaimsHelper
{
    public static Guid? GetUserIdOrNull(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AppClaims.Subject), out var id) ? id : null;

    public static Guid GetUserId(ClaimsPrincipal user) =>
        GetUserIdOrNull(user) ?? throw new UnauthorizedException();

    public static string GetTokenId(ClaimsPrincipal user) =>
        user.FindFirstValue(AppClaims.TokenId) ?? throw new UnauthorizedException();

    public static int? GetSessionVersion(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(AppClaims.SessionVersion), out var version) ? version : null;

    public static DateTime GetTokenExpiresAt(ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(AppClaims.ExpiresAt), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : throw new UnauthorizedException();
}
