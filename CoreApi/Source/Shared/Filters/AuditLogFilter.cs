namespace LittleEcommerce.Source.Shared.Filters;

public class AuditLogFilter : BaseFilter
{
    public string? Kind { get; set; }

    public string? Action { get; set; }

    public string? ActorId { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}
