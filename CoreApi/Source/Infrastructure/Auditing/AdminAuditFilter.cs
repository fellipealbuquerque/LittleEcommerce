using System.Diagnostics;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public sealed class AdminAuditFilter(IAuditWriter writer, ICurrentActor actor, TimeProvider clock) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var started = Stopwatch.GetTimestamp();
        var arguments = AuditSanitizer.Arguments(context.ActionArguments);
        var request = context.HttpContext.Request;

        var executed = await next();
        var status = StatusCodeOf(executed);

        writer.Enqueue(new AuditEntry
        {
            Timestamp = clock.GetUtcNow().UtcDateTime,
            Kind = AuditKinds.AdminAction,
            Action = ActionName(context.ActionDescriptor as ControllerActionDescriptor),
            Actor = actor.Snapshot(),
            Permission = RequiredPermissions(context.ActionDescriptor.EndpointMetadata),
            Request = new AuditRequest
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.HasValue ? SensitiveData.MaskQueryString(request.QueryString) : null,
                Arguments = arguments
            },
            StatusCode = status,
            Succeeded = status < StatusCodes.Status400BadRequest,
            DurationMs = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 2),
            CorrelationId = context.HttpContext.TraceIdentifier
        });
    }

    internal static int StatusCodeOf(ActionExecutedContext executed) => executed switch
    {
        { Exception: AppException app, ExceptionHandled: false } => app.StatusCode,
        { Exception: not null, ExceptionHandled: false } => StatusCodes.Status500InternalServerError,
        { Result: IStatusCodeActionResult { StatusCode: { } code } } => code,
        _ => executed.HttpContext.Response.StatusCode
    };

    private static string ActionName(ControllerActionDescriptor? descriptor) =>
        descriptor is null ? "unknown" : $"{descriptor.ControllerName}.{descriptor.ActionName}";

    private static string? RequiredPermissions(IEnumerable<object> metadata)
    {
        var permissions = metadata.OfType<RequiresPermissionAttribute>().Select(p => p.Permission).Distinct().ToList();
        return permissions.Count == 0 ? null : string.Join(',', permissions);
    }
}
