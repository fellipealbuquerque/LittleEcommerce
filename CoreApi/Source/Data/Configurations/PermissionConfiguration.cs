using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LittleEcommerce.Source.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasIndex(p => p.Code).IsUnique();

        builder.HasData(Permissions.Catalog.Select(p => new Permission
        {
            Id = p.Id,
            Code = p.Code,
            Group = p.Group,
            Description = p.Description,
            CreatedAt = RoleConfiguration.SeededAt,
            UpdatedAt = RoleConfiguration.SeededAt
        }));
    }
}
