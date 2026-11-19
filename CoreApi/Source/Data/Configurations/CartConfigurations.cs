using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LittleEcommerce.Source.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasIndex(c => c.UserId).IsUnique();

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => c.User.DeletedAt == null);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.Property(i => i.UnitPriceWhenAdded).HasPrecision(12, 2);

        builder.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        builder.HasIndex(i => i.ProductId);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_cart_items_quantity",
            $"quantity BETWEEN 1 AND {CartLimits.MaxItemQuantity}"));

        builder.HasOne(i => i.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(i => i.Product.DeletedAt == null);
    }
}
