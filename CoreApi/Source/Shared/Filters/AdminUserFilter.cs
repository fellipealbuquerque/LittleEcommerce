using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Shared.Filters;

public class AdminUserFilter : BaseFilter
{
    public string? Search { get; set; }

    public string? Role { get; set; }

    public UserStatus? Status { get; set; }
}
