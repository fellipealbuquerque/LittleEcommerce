using LittleEcommerce.Source.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LittleEcommerce.Source.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasIndex(c => c.Slug).IsUnique().HasFilter("deleted_at IS NULL");
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Price).HasPrecision(12, 2);
        builder.Property(p => p.CompareAtPrice).HasPrecision(12, 2);
        builder.Property(p => p.Version).IsRowVersion();

        builder.HasIndex(p => p.Sku).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(p => new { p.IsActive, p.CategoryId, p.Price });
        builder.HasIndex(p => p.Name).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_products_price_positive", "price > 0");
            t.HasCheckConstraint("ck_products_stock_not_negative", "stock >= 0");
            t.HasCheckConstraint("ck_products_compare_at_price", "compare_at_price IS NULL OR compare_at_price > price");
        });

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductLinkConfiguration : IEntityTypeConfiguration<ProductLink>
{
    public void Configure(EntityTypeBuilder<ProductLink> builder)
    {
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => new { l.ProductId, l.LinkedProductId, l.Type }).IsUnique();
        builder.HasIndex(l => l.LinkedProductId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_product_links_not_self", "product_id <> linked_product_id");
            t.HasCheckConstraint("ck_product_links_quantity_positive", "quantity > 0");
        });

        builder.HasOne(l => l.Product)
            .WithMany(p => p.Links)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.LinkedProduct)
            .WithMany()
            .HasForeignKey(l => l.LinkedProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(l => l.Product.DeletedAt == null && l.LinkedProduct.DeletedAt == null);
    }
}
