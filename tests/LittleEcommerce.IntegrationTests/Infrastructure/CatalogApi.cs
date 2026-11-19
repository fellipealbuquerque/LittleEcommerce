using System.Text.Json;

namespace LittleEcommerce.IntegrationTests.Infrastructure;

public static class CatalogApi
{
    public static string Token() => Guid.NewGuid().ToString("N")[..8];

    public static async Task<JsonElement> CreateCategoryAsync(this HttpClient client, Session admin, string name, bool isActive = true)
    {
        var response = await client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/categories", admin.AccessToken, new { name, isActive });
        return await DataOrThrowAsync(response);
    }

    public static async Task<JsonElement> CreateProductAsync(
        this HttpClient client,
        Session admin,
        string sku,
        string name,
        decimal price,
        int stock = 10,
        string? categoryId = null,
        decimal? compareAtPrice = null,
        bool isActive = true,
        string? description = null)
    {
        var response = await client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/products", admin.AccessToken,
            new { sku, name, price, stock, categoryId, compareAtPrice, isActive, description });
        return await DataOrThrowAsync(response);
    }

    public static async Task<JsonElement> ListPublicAsync(this HttpClient client, string query) =>
        await (await client.GetAsync($"/api/v1/products?{query}")).ReadJsonAsync();

    public static async Task<JsonElement> DataOrThrowAsync(HttpResponseMessage response)
    {
        var body = await response.ReadJsonAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{(int)response.StatusCode}: {body}");

        return body.GetProperty("data");
    }

    public static string Id(this JsonElement element) => element.GetProperty("id").GetString()!;

    public static IReadOnlyList<string> Names(this JsonElement page) =>
        page.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("name").GetString()!).ToList();
}
