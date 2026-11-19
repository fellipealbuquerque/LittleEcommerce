using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.UnitTests.Support;

internal static class TestUsers
{
    public static readonly Role Customer = new() { Id = Roles.CustomerId, Name = Roles.Customer };

    public static User Create(UserStatus status = UserStatus.Active, int sessionVersion = 1) => new()
    {
        Name = "Ana",
        Email = "ana@loja.com",
        NormalizedEmail = "ana@loja.com",
        PasswordHash = "hash-atual",
        RoleId = Customer.Id,
        Role = Customer,
        Status = status,
        SessionVersion = sessionVersion
    };
}
