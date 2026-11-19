namespace LittleEcommerce.Source.Common.Enums;

public enum ProductLinkType
{
    Related,
    Accessory,
    Kit,
    Substitute
}

public static class ProductLinkTypeRules
{
    public static bool IsSymmetric(this ProductLinkType type) =>
        type is ProductLinkType.Related or ProductLinkType.Substitute;

    public static bool HasQuantity(this ProductLinkType type) => type == ProductLinkType.Kit;
}
