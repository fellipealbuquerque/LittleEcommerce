using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Application.Caching;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.Filters;
using LittleEcommerce.Source.Shared.Helpers;

namespace LittleEcommerce.UnitTests.Catalog;

public class CatalogHelpersTests
{
    [Theory]
    [InlineData("Notebook Ultra 14\"", "notebook-ultra-14")]
    [InlineData("Ação & Café — Edição Única", "acao-cafe-edicao-unica")]
    [InlineData("  --Mouse   sem fio--  ", "mouse-sem-fio")]
    [InlineData("!!!", "item")]
    public void Slugify_ShouldProduceUrlSafeSlug(string input, string expected)
    {
        Assert.Equal(expected, Slugifier.Slugify(input));
    }

    [Fact]
    public void Slugify_ShouldRespectMaxLengthWithoutTrailingDash()
    {
        var slug = Slugifier.Slugify("aaaa bbbb cccc", 10);

        Assert.Equal("aaaa-bbbb", slug);
    }

    [Fact]
    public void LikePattern_ShouldEscapeWildcards()
    {
        Assert.Equal("%50\\% off\\_promo%", LikePattern.Contains(" 50% off_promo "));
    }

    [Theory]
    [InlineData(899.90, 1099.90, 18)]
    [InlineData(50.0, 100.0, 50)]
    [InlineData(100.0, null, null)]
    public void DiscountPercent_ShouldRoundRelativeToOriginalPrice(double price, double? compareAt, int? expected)
    {
        var product = new Product { Price = (decimal)price, CompareAtPrice = (decimal?)compareAt };

        Assert.Equal(expected, CatalogMapper.DiscountPercent(product));
    }

    [Fact]
    public void ProductFilter_ShouldRejectMinPriceAboveMaxPrice()
    {
        var filter = new ProductFilter { MinPrice = 50, MaxPrice = 10 };

        var errors = filter.Validate(new ValidationContext(filter)).ToList();

        Assert.Single(errors);
        Assert.Contains(nameof(ProductFilter.MinPrice), errors[0].MemberNames);
    }

    [Fact]
    public void ProductListKey_ShouldIgnoreCaseAndSpacesInSearch()
    {
        var a = CatalogCacheKeys.ProductList(new ProductFilter { Search = " Mouse " });
        var b = CatalogCacheKeys.ProductList(new ProductFilter { Search = "mouse" });
        var c = CatalogCacheKeys.ProductList(new ProductFilter { Search = "mouse", Sort = ProductSort.PriceAsc });

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
