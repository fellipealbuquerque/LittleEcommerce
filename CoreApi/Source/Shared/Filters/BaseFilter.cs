using System.ComponentModel.DataAnnotations;

namespace LittleEcommerce.Source.Shared.Filters;

public class BaseFilter
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    [Range(1, MaxLimit, ErrorMessage = "limit precisa estar entre 1 e 100")]
    public int Limit { get; set; } = DefaultLimit;

    [Range(0, int.MaxValue, ErrorMessage = "offset não pode ser negativo")]
    public int Offset { get; set; }
}
