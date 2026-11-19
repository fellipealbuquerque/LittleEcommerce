using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LittleEcommerce.Source.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly DateTime SeededAt = new(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new Role { Id = Roles.AdminId, Name = Roles.Admin, Description = "Acesso total à administração", CreatedAt = SeededAt, UpdatedAt = SeededAt },
            new Role { Id = Roles.ManagerId, Name = Roles.Manager, Description = "Gestão de catálogo, pedidos e relatórios", CreatedAt = SeededAt, UpdatedAt = SeededAt },
            new Role { Id = Roles.CustomerId, Name = Roles.Customer, Description = "Cliente da loja", CreatedAt = SeededAt, UpdatedAt = SeededAt });
    }
}
