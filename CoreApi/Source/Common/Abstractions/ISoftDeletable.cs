namespace LittleEcommerce.Source.Common.Abstractions;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
