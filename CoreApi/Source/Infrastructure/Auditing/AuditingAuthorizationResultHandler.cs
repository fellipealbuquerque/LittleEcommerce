using LittleEcommerce.Source.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public sealed class AuditingAuthorizationResultHandler(IAuditWriter writer, ICurrentActor actor, TimeProvider clock)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
            writer.Enqueue(Denied(context, authorizeResult));

        return _default.HandleAsync(next, context, policy, authorizeResult);
    }

    private AuditEntry Denied(HttpContext context, PolicyAuthorizationResult result)
    {
        var missing = result.AuthorizationFailure?.FailedRequirements
            .OfType<PermissionRequirement>()
            .Select(r => r.Permission)
            .ToList() ?? [];

        return new AuditEntry
        {
            Timestamp = clock.GetUtcNow().UtcDateTime,
            Kind = AuditKinds.AccessDenied,
            Action = $"{context.Request.Method} {context.Request.Path}",
            Actor = actor.Snapshot(),
            Permission = missing.Count == 0 ? null : string.Join(',', missing),
            Request = new AuditRequest { Method = context.Request.Method, Path = context.Request.Path },
            StatusCode = StatusCodes.Status403Forbidden,
            Succeeded = false,
            CorrelationId = context.TraceIdentifier
        };
    }
}
