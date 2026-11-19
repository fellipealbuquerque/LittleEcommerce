using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Caching;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LittleEcommerce.UnitTests.Catalog;

public class ProductAdminServiceTests
{
    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IProductLinkRepository> _links = new();
    private readonly Mock<ICatalogCache> _cache = new();

    private ProductAdminService CreateService() => new(
        _unitOfWork,
        _products.Object,
        _categories.Object,
        _links.Object,
        _cache.Object,
        new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero)),
        NullLogger<ProductAdminService>.Instance);

    private static CreateProductRequest Request(
        string sku = "nb-01",
        string name = "Notebook 14",
        string? slug = null,
        decimal price = 100m,
        decimal? compareAt = null) =>
        new(sku, name, slug, null, price, compareAt, 3, null, null);

    private Product Existing(string name = "Produto")
    {
        var product = new Product { Sku = "SKU-" + name.ToUpperInvariant(), Name = name, Slug = name.ToLowerInvariant(), Price = 10m };
        _products.Setup(p => p.GetAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        return product;
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateSkuIgnoringCase()
    {
        _products.Setup(p => p.SkuExistsAsync("NB-01", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Request()));

        Assert.Equal(ErrorCodes.SkuAlreadyExists, ex.Code);
    }

    [Theory]
    [InlineData(100.0, 100.0, "compareAtPrice")]
    [InlineData(100.0, 90.0, "compareAtPrice")]
    [InlineData(10.555, null, "price")]
    public async Task CreateAsync_ShouldValidatePrices(double price, double? compareAt, string field)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().CreateAsync(Request(price: (decimal)price, compareAt: (decimal?)compareAt)));

        Assert.True(ex.Errors.ContainsKey(field));
    }

    [Fact]
    public async Task CreateAsync_ShouldSuffixGeneratedSlug_WhenTaken()
    {
        _products.Setup(p => p.SlugExistsAsync("notebook-14", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _products.Setup(p => p.SlugExistsAsync("notebook-14-2", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().CreateAsync(Request());

        Assert.Equal("notebook-14-3", result.Slug);
        Assert.Equal("NB-01", result.Sku);
        _cache.Verify(c => c.InvalidateAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectExplicitSlugAlreadyInUse()
    {
        _products.Setup(p => p.SlugExistsAsync("meu-slug", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Request(slug: "meu-slug")));

        Assert.Equal(ErrorCodes.SlugAlreadyExists, ex.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnknownCategory()
    {
        var request = Request() with { CategoryId = Guid.NewGuid() };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateService().CreateAsync(request));

        Assert.True(ex.Errors.ContainsKey("categoryId"));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPinTheVersionTheClientRead()
    {
        var product = Existing();

        await CreateService().UpdateAsync(product.Id, new UpdateProductRequest("Novo nome", null, null, 20m, null, 1, null, null, true, 77));

        _products.Verify(p => p.ExpectVersion(product, 77u), Times.Once);
        Assert.Equal("Novo nome", product.Name);
        Assert.Equal("produto", product.Slug);
    }

    [Fact]
    public async Task AddLinkAsync_ShouldRefuseLinkingProductToItself()
    {
        var product = Existing();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().AddLinkAsync(product.Id, new CreateProductLinkRequest(product.Id, ProductLinkType.Related)));

        Assert.True(ex.Errors.ContainsKey("linkedProductId"));
    }

    [Fact]
    public async Task AddLinkAsync_ShouldCreateReverseLink_ForSymmetricTypes()
    {
        var product = Existing("A");
        var other = Existing("B");
        var added = new List<ProductLink>();
        _links.Setup(l => l.Add(It.IsAny<ProductLink>())).Callback<ProductLink>(added.Add);

        await CreateService().AddLinkAsync(product.Id, new CreateProductLinkRequest(other.Id, ProductLinkType.Related, Quantity: 5));

        Assert.Equal(2, added.Count);
        Assert.Contains(added, l => l.ProductId == product.Id && l.LinkedProductId == other.Id);
        Assert.Contains(added, l => l.ProductId == other.Id && l.LinkedProductId == product.Id);
        Assert.All(added, l => Assert.Equal(1, l.Quantity));
    }

    [Fact]
    public async Task AddLinkAsync_ShouldKeepQuantityOnlyForKits_AndStayOneWay()
    {
        var kit = Existing("Kit");
        var part = Existing("Peca");
        var added = new List<ProductLink>();
        _links.Setup(l => l.Add(It.IsAny<ProductLink>())).Callback<ProductLink>(added.Add);

        var result = await CreateService().AddLinkAsync(kit.Id, new CreateProductLinkRequest(part.Id, ProductLinkType.Kit, Quantity: 2));

        Assert.Single(added);
        Assert.Equal(2, result.Quantity);
    }

    [Fact]
    public async Task AddLinkAsync_ShouldRejectDuplicates()
    {
        var product = Existing("A");
        var other = Existing("B");
        _links.Setup(l => l.FindAsync(product.Id, other.Id, ProductLinkType.Accessory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductLink());

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().AddLinkAsync(product.Id, new CreateProductLinkRequest(other.Id, ProductLinkType.Accessory)));

        Assert.Equal(ErrorCodes.LinkAlreadyExists, ex.Code);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEveryLinkTouchingTheProduct()
    {
        var product = Existing();
        var touching = new List<ProductLink> { new(), new() };
        _links.Setup(l => l.ListTouchingAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(touching);

        await CreateService().DeleteAsync(product.Id);

        _links.Verify(l => l.Remove(It.IsAny<ProductLink>()), Times.Exactly(2));
        _products.Verify(p => p.Remove(product), Times.Once);
        _cache.Verify(c => c.InvalidateAsync(), Times.Once);
    }
}
