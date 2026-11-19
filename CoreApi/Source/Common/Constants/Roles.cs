namespace LittleEcommerce.Source.Common.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Customer = "Customer";

    public static readonly Guid AdminId = Guid.Parse("019a9c00-0000-7000-8000-000000000001");
    public static readonly Guid ManagerId = Guid.Parse("019a9c00-0000-7000-8000-000000000002");
    public static readonly Guid CustomerId = Guid.Parse("019a9c00-0000-7000-8000-000000000003");
}
