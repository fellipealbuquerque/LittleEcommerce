using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;

namespace LittleEcommerce.IntegrationTests.Catalog;

[Collection(IntegrationCollection.Name)]
public class CatalogTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task PublicList_ShouldFilterSearchPriceStockAndSort()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        await _client.CreateProductAsync(admin, $"A-{t}", $"{t} Alfa", 10m, stock: 5);
        await _client.CreateProductAsync(admin, $"B-{t}", $"{t} Bravo", 20m, stock: 0);
        await _client.CreateProductAsync(admin, $"C-{t}", $"{t} Charlie", 30m, stock: 5);
        await _client.CreateProductAsync(admin, $"D-{t}", $"{t} Delta", 40m, stock: 5, isActive: false);

        var all = await _client.ListPublicAsync($"search={t}");
        var range = await _client.ListPublicAsync($"search={t}&minPrice=15&maxPrice=35");
        var inStock = await _client.ListPublicAsync($"search={t}&inStock=true&sort=PriceDesc");
        var bySku = await _client.ListPublicAsync($"search=b-{t}");
        var page = await _client.ListPublicAsync($"search={t}&sort=NameAsc&limit=2&offset=2");

        Assert.Equal(3, all.GetProperty("total").GetInt32());
        Assert.Equal([$"{t} Charlie", $"{t} Bravo"], range.Names().OrderByDescending(n => n).ToList());
        Assert.Equal([$"{t} Charlie", $"{t} Alfa"], inStock.Names());
        Assert.Equal([$"{t} Bravo"], bySku.Names());
        Assert.Equal([$"{t} Charlie"], page.Names());
        Assert.Equal(2, page.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task PublicList_ShouldFilterByCategorySlugAndHideInactiveCategories()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var category = await _client.CreateCategoryAsync(admin, $"Categoria {t}");
        await _client.CreateProductAsync(admin, $"CAT-{t}", $"{t} na categoria", 50m, categoryId: category.Id());
        await _client.CreateProductAsync(admin, $"SEM-{t}", $"{t} sem categoria", 50m);

        var bySlug = await _client.ListPublicAsync($"category=categoria-{t}");
        var categories = (await (await _client.GetAsync("/api/v1/categories")).ReadJsonAsync()).GetProperty("data");

        await _client.SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/categories/{category.Id()}", admin.AccessToken,
            new { name = $"Categoria {t}", isActive = false });
        var hidden = await _client.ListPublicAsync($"search={t}");

        Assert.Equal([$"{t} na categoria"], bySlug.Names());
        Assert.Contains(categories.EnumerateArray(), c => c.GetProperty("slug").GetString() == $"categoria-{t}" && c.GetProperty("products").GetInt32() == 1);
        Assert.Equal([$"{t} sem categoria"], hidden.Names());
    }

    [Fact]
    public async Task ProductDetail_ShouldWorkByIdAndSlug_AndHideInactive()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var product = await _client.CreateProductAsync(admin, $"DET-{t}", $"Detalhe {t}", 80m, compareAtPrice: 100m, description: "Bom produto");

        var byId = await _client.GetAsync($"/api/v1/products/{product.Id()}");
        var bySlug = await (await _client.GetAsync($"/api/v1/products/detalhe-{t}")).ReadJsonAsync();

        await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/products/{product.Id()}/status", admin.AccessToken, new { isActive = false });
        var afterDeactivation = await _client.GetAsync($"/api/v1/products/{product.Id()}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(20, bySlug.GetProperty("data").GetProperty("discountPercent").GetInt32());
        Assert.Equal("Bom produto", bySlug.GetProperty("data").GetProperty("description").GetString());
        Assert.Equal(HttpStatusCode.NotFound, afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task Links_ShouldBeMutualForRelatedAndOneWayForKits()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var kit = await _client.CreateProductAsync(admin, $"KIT-{t}", $"Kit {t}", 300m);
        var mouse = await _client.CreateProductAsync(admin, $"MOU-{t}", $"Mouse {t}", 100m);
        var keyboard = await _client.CreateProductAsync(admin, $"TEC-{t}", $"Teclado {t}", 150m);

        var kitLink = await CatalogApi.DataOrThrowAsync(await _client.SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/admin/products/{kit.Id()}/links", admin.AccessToken,
            new { linkedProductId = mouse.Id(), type = "Kit", quantity = 2 }));
        var related = await CatalogApi.DataOrThrowAsync(await _client.SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/admin/products/{mouse.Id()}/links", admin.AccessToken,
            new { linkedProductId = keyboard.Id(), type = "Related" }));

        var kitLinks = (await (await _client.GetAsync($"/api/v1/products/{kit.Id()}/links")).ReadJsonAsync()).GetProperty("data");
        var mouseLinks = (await (await _client.GetAsync($"/api/v1/products/{mouse.Id()}/links")).ReadJsonAsync()).GetProperty("data");
        var keyboardLinks = (await (await _client.GetAsync($"/api/v1/products/{keyboard.Id()}/links?type=Related")).ReadJsonAsync()).GetProperty("data");

        Assert.Equal(2, kitLink.GetProperty("quantity").GetInt32());
        Assert.Equal(mouse.Id(), kitLinks[0].GetProperty("product").Id());
        Assert.Equal("Kit", kitLinks[0].GetProperty("type").GetString());
        Assert.DoesNotContain(mouseLinks.EnumerateArray(), l => l.GetProperty("type").GetString() == "Kit");
        Assert.Equal(mouse.Id(), keyboardLinks.EnumerateArray().Single().GetProperty("product").Id());

        await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/products/{mouse.Id()}/links/{related.Id()}", admin.AccessToken);
        var keyboardAfter = (await (await _client.GetAsync($"/api/v1/products/{keyboard.Id()}/links")).ReadJsonAsync()).GetProperty("data");

        Assert.Equal(0, keyboardAfter.GetArrayLength());
    }

    [Fact]
    public async Task CacheShouldBeInvalidated_WhenCatalogChanges()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var product = await _client.CreateProductAsync(admin, $"CACHE-{t}", $"{t} Cache", 10m, stock: 3);

        var before = await _client.ListPublicAsync($"search={t}");
        await _client.CreateProductAsync(admin, $"CACHE2-{t}", $"{t} Cache dois", 10m);
        await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/products/{product.Id()}/stock", admin.AccessToken, new { quantity = 0 });
        var after = await _client.ListPublicAsync($"search={t}&sort=NameAsc");

        Assert.Equal(1, before.GetProperty("total").GetInt32());
        Assert.Equal(2, after.GetProperty("total").GetInt32());
        Assert.False(after.GetProperty("data")[0].GetProperty("inStock").GetBoolean());
    }

    [Fact]
    public async Task Update_ShouldRejectStaleVersion()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var product = await _client.CreateProductAsync(admin, $"VER-{t}", $"Versao {t}", 10m);
        var version = product.GetProperty("version").GetUInt32();

        object Body(decimal price) => new { name = $"Versao {t}", price, stock = 1, isActive = true, version };

        var first = await _client.SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/products/{product.Id()}", admin.AccessToken, Body(11m));
        var stale = await _client.SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/products/{product.Id()}", admin.AccessToken, Body(12m));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotEqual(version, (await first.ReadJsonAsync()).GetProperty("data").GetProperty("version").GetUInt32());
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", await stale.ErrorCodeAsync());
    }

    [Fact]
    public async Task Create_ShouldValidateAndKeepIdentifiersUnique()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var first = await _client.CreateProductAsync(admin, $"uni-{t}", $"Unico {t}", 10m);
        var second = await _client.CreateProductAsync(admin, $"UNI2-{t}", $"Unico {t}", 10m);

        var duplicateSku = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/products", admin.AccessToken,
            new { sku = $"UNI-{t}", name = "Outro", price = 5m, stock = 1 });
        var badPrice = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/products", admin.AccessToken,
            new { sku = $"PRC-{t}", name = "Preco", price = 50m, compareAtPrice = 40m, stock = 1 });

        Assert.Equal($"UNI-{t}".ToUpperInvariant(), first.GetProperty("sku").GetString());
        Assert.Equal($"unico-{t}", first.GetProperty("slug").GetString());
        Assert.Equal($"unico-{t}-2", second.GetProperty("slug").GetString());
        Assert.Equal("SKU_ALREADY_EXISTS", await duplicateSku.ErrorCodeAsync());
        Assert.True((await badPrice.ReadJsonAsync()).GetProperty("errors").TryGetProperty("compareAtPrice", out _));
    }

    [Fact]
    public async Task Delete_ShouldHideProductAndKeepCategoryProtected()
    {
        var admin = await _client.LoginAdminAsync();
        var t = CatalogApi.Token();
        var category = await _client.CreateCategoryAsync(admin, $"Remover {t}");
        var product = await _client.CreateProductAsync(admin, $"DEL-{t}", $"Remover {t}", 10m, categoryId: category.Id());

        var categoryInUse = await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/categories/{category.Id()}", admin.AccessToken);
        await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/products/{product.Id()}", admin.AccessToken);
        var publicDetail = await _client.GetAsync($"/api/v1/products/{product.Id()}");
        var adminDetail = await _client.SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/admin/products/{product.Id()}", admin.AccessToken);
        var categoryDeleted = await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/categories/{category.Id()}", admin.AccessToken);

        Assert.Equal("CATEGORY_IN_USE", await categoryInUse.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.NotFound, publicDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categoryDeleted.StatusCode);
    }

    [Fact]
    public async Task CatalogAdmin_ShouldRequirePermission_AndManagerCanManage()
    {
        var admin = await _client.LoginAdminAsync();
        var customer = await _client.RegisterAsync();
        var manager = await _client.RegisterWithRoleAsync(admin, "Manager");
        var t = CatalogApi.Token();

        var denied = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/products", customer.AccessToken,
            new { sku = $"NEG-{t}", name = "Negado", price = 5m, stock = 1 });
        var allowed = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/products", manager.AccessToken,
            new { sku = $"MGR-{t}", name = $"Gerente {t}", price = 5m, stock = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
    }
}
