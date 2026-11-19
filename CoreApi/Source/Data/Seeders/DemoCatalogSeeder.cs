using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Seeders;

public class DemoCatalogSeeder(ApplicationDbContext context, TimeProvider clock, ILogger<DemoCatalogSeeder> logger)
{
    private sealed record Item(string Sku, string Name, string Category, decimal Price, decimal? CompareAt, int Stock, bool Active = true, string? Description = null);

    private static readonly (string Name, string Description)[] Categories =
    [
        ("Eletrônicos", "Notebooks, monitores e fones"),
        ("Acessórios", "Periféricos e cabos"),
        ("Casa e Escritório", "Móveis e organização")
    ];

    private static readonly Item[] Items =
    [
        new("NB-ULTRA-14", "Notebook Ultra 14", "Eletrônicos", 5499.90m, 5999.90m, 12, Description: "14 polegadas, 16 GB de RAM e SSD de 512 GB"),
        new("NB-PRO-16", "Notebook Pro 16", "Eletrônicos", 8999.00m, null, 5, Description: "16 polegadas para trabalho pesado"),
        new("MON-27-4K", "Monitor 27 4K", "Eletrônicos", 2199.00m, 2499.00m, 8),
        new("FONE-BT-NC", "Fone Bluetooth com cancelamento de ruído", "Eletrônicos", 899.90m, 1099.90m, 20),
        new("FONE-BT-SPORT", "Fone Bluetooth Sport", "Eletrônicos", 349.90m, null, 0, Description: "Esgotado no momento"),
        new("MOUSE-ERGO", "Mouse ergonômico sem fio", "Acessórios", 249.90m, null, 40),
        new("TECLADO-MEC", "Teclado mecânico ABNT2", "Acessórios", 499.90m, 599.90m, 15),
        new("HUB-USBC-7", "Hub USB-C 7 em 1", "Acessórios", 189.90m, null, 30),
        new("CABO-HDMI-2M", "Cabo HDMI 2.1 de 2 m", "Acessórios", 79.90m, null, 60),
        new("SUPORTE-NB", "Suporte de notebook em alumínio", "Casa e Escritório", 159.90m, null, 25),
        new("CADEIRA-ERGO", "Cadeira ergonômica", "Casa e Escritório", 1899.00m, 2299.00m, 4),
        new("KIT-HOME-OFFICE", "Kit Home Office", "Casa e Escritório", 899.00m, 989.60m, 10, Description: "Teclado, mouse, hub e suporte"),
        new("LUMINARIA-LED", "Luminária de mesa LED", "Casa e Escritório", 129.90m, null, 18, Active: false)
    ];

    private static readonly (string From, string To, ProductLinkType Type, int Quantity)[] Links =
    [
        ("NB-ULTRA-14", "MOUSE-ERGO", ProductLinkType.Accessory, 1),
        ("NB-ULTRA-14", "HUB-USBC-7", ProductLinkType.Accessory, 1),
        ("NB-ULTRA-14", "SUPORTE-NB", ProductLinkType.Accessory, 1),
        ("NB-PRO-16", "MON-27-4K", ProductLinkType.Accessory, 1),
        ("NB-ULTRA-14", "NB-PRO-16", ProductLinkType.Related, 1),
        ("FONE-BT-NC", "FONE-BT-SPORT", ProductLinkType.Substitute, 1),
        ("MON-27-4K", "CABO-HDMI-2M", ProductLinkType.Accessory, 1),
        ("KIT-HOME-OFFICE", "TECLADO-MEC", ProductLinkType.Kit, 1),
        ("KIT-HOME-OFFICE", "MOUSE-ERGO", ProductLinkType.Kit, 1),
        ("KIT-HOME-OFFICE", "HUB-USBC-7", ProductLinkType.Kit, 1),
        ("KIT-HOME-OFFICE", "SUPORTE-NB", ProductLinkType.Kit, 1)
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Products.IgnoreQueryFilters().AnyAsync(ct))
            return;

        var now = clock.GetUtcNow().UtcDateTime;

        var categories = Categories.ToDictionary(
            c => c.Name,
            c => new Category { Name = c.Name, Slug = Slugifier.Slugify(c.Name, 100), Description = c.Description });

        var products = Items
            .Select((item, index) => new Product
            {
                Sku = item.Sku,
                Name = item.Name,
                Slug = Slugifier.Slugify(item.Name),
                Description = item.Description,
                Price = item.Price,
                CompareAtPrice = item.CompareAt,
                Stock = item.Stock,
                IsActive = item.Active,
                Category = categories[item.Category],
                CreatedAt = now.AddMinutes(-index)
            })
            .ToDictionary(p => p.Sku);

        context.Categories.AddRange(categories.Values);
        context.Products.AddRange(products.Values);

        foreach (var (from, to, type, quantity) in Links)
        {
            context.ProductLinks.Add(new ProductLink { Product = products[from], LinkedProduct = products[to], Type = type, Quantity = quantity, CreatedAt = now });

            if (type.IsSymmetric())
                context.ProductLinks.Add(new ProductLink { Product = products[to], LinkedProduct = products[from], Type = type, Quantity = 1, CreatedAt = now });
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Catálogo de demonstração criado: {Categories} categorias e {Products} produtos", categories.Count, products.Count);
    }
}
