using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Shared.Filters;

public class OutboxFilter : BaseFilter
{
    public OutboxStatus? Status { get; set; }

    public string? Type { get; set; }

    public string? AggregateId { get; set; }
}
