using System.Text.Json;

namespace LittleEcommerce.IntegrationTests.Infrastructure;

public static class AdminApi
{
    private static readonly TimeSpan AuditWaitLimit = TimeSpan.FromSeconds(15);

    public static Task<Session> LoginAdminAsync(this HttpClient client) =>
        client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

    public static async Task<Session> RegisterWithRoleAsync(this HttpClient client, Session admin, string role)
    {
        var email = AuthApi.NewEmail();
        var registered = await client.RegisterAsync(email);

        var change = await client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{registered.UserId}/role", admin.AccessToken, new { role });

        if (!change.IsSuccessStatusCode)
            throw new InvalidOperationException($"Falha ao trocar papel: {await change.Content.ReadAsStringAsync()}");

        return await client.LoginAsync(email);
    }

    public static async Task<JsonElement> WaitForAuditAsync(this HttpClient client, Session admin, string query, Func<JsonElement, bool> match)
    {
        var deadline = DateTime.UtcNow + AuditWaitLimit;

        while (DateTime.UtcNow < deadline)
        {
            var response = await client.SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/admin/audit-logs?{query}", admin.AccessToken);
            var data = (await response.ReadJsonAsync()).GetProperty("data");

            foreach (var item in data.EnumerateArray())
            {
                if (match(item))
                    return item;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Nenhum registro de auditoria para '{query}' em {AuditWaitLimit.TotalSeconds}s");
    }
}
