using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Shared.Extensions;

public static class ControllerExtensions
{
    public static Guid UserId(this ControllerBase controller) =>
        ClaimsHelper.GetUserId(controller.User);

    public static string TokenId(this ControllerBase controller) =>
        ClaimsHelper.GetTokenId(controller.User);

    public static DateTime TokenExpiresAt(this ControllerBase controller) =>
        ClaimsHelper.GetTokenExpiresAt(controller.User);

    public static ClientInfo Client(this ControllerBase controller) =>
        new(
            controller.HttpContext.Connection.RemoteIpAddress?.ToString(),
            controller.Request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? agent : null);
}
