using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LittleEcommerce.Source.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    private static readonly Dictionary<string, Guid> RoleIds = new()
    {
        [Roles.Admin] = Roles.AdminId,
        [Roles.Manager] = Roles.ManagerId,
        [Roles.Customer] = Roles.CustomerId
    };

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(Permissions.DefaultsByRole.SelectMany(entry => entry.Value.Select(code => new RolePermission
        {
            RoleId = RoleIds[entry.Key],
            PermissionId = Permissions.Get(code).Id
        })));
    }
}
