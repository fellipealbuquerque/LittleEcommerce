using System.Security.Claims;
using LittleEcommerce.Source.Common.Constants;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public interface ICurrentActor
{
    AuditActor Snapshot();

    string? CorrelationId { get; }
}

public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private const int UserAgentMaxLength = 300;

    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;

    public AuditActor Snapshot()
    {
        var http = accessor.HttpContext;

        if (http is null)
            return new AuditActor { Type = AuditActorTypes.System };

        var userId = http.User.FindFirstValue(AppClaims.Subject);
        var userAgent = http.Request.Headers.UserAgent.ToString();

        return new AuditActor
        {
            Type = userId is null ? AuditActorTypes.Anonymous : AuditActorTypes.User,
            UserId = userId,
            Email = http.User.FindFirstValue(AppClaims.Email),
            Role = http.User.FindFirstValue(AppClaims.Role),
            Ip = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent.Length > UserAgentMaxLength ? userAgent[..UserAgentMaxLength] : userAgent.Length == 0 ? null : userAgent
        };
    }
}
