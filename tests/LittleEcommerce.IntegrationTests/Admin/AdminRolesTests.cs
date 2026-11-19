using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Common.Constants;

namespace LittleEcommerce.IntegrationTests.Admin;

[Collection(IntegrationCollection.Name)]
public class AdminRolesTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task Roles_ShouldListSystemRolesWithAdminHoldingEveryPermission()
    {
        var admin = await _client.LoginAdminAsync();

        var roles = (await (await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/roles", admin.AccessToken)).ReadJsonAsync()).GetProperty("data");
        var catalog = (await (await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/permissions", admin.AccessToken)).ReadJsonAsync()).GetProperty("data");

        var adminRole = roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == Roles.Admin);
        var managerRole = roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == Roles.Manager);

        Assert.Equal(catalog.GetArrayLength(), adminRole.GetProperty("permissions").GetArrayLength());
        Assert.True(adminRole.GetProperty("isSystem").GetBoolean());
        Assert.Contains(managerRole.GetProperty("permissions").EnumerateArray(), p => p.GetString() == Permissions.UsersRead);
        Assert.True(adminRole.GetProperty("users").GetInt32() >= 1);
    }

    [Fact]
    public async Task CustomRole_ShouldGrantAndRevokeAccessImmediately()
    {
        var admin = await _client.LoginAdminAsync();
        var name = $"Auditor{Guid.NewGuid():N}"[..20];

        var created = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/roles", admin.AccessToken,
            new { name, description = "Lê auditoria", permissions = new[] { Permissions.AuditRead } });
        var roleId = (await created.ReadJsonAsync()).GetProperty("data").GetProperty("id").GetString();
        var auditor = await _client.RegisterWithRoleAsync(admin, name);

        var allowed = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/audit-logs?limit=1", auditor.AccessToken);

        await _client.SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/roles/{roleId}/permissions", admin.AccessToken, new { permissions = Array.Empty<string>() });
        var denied = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/audit-logs?limit=1", auditor.AccessToken);

        var inUse = await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/roles/{roleId}", admin.AccessToken);
        await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{auditor.UserId}/role", admin.AccessToken, new { role = Roles.Customer });
        var deleted = await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/roles/{roleId}", admin.AccessToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, inUse.StatusCode);
        Assert.Equal("ROLE_IN_USE", await inUse.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
    }

    [Fact]
    public async Task SystemRoles_ShouldBeProtected()
    {
        var admin = await _client.LoginAdminAsync();

        var deleteCustomer = await _client.SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/roles/{Roles.CustomerId}", admin.AccessToken);
        var editAdmin = await _client.SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/roles/{Roles.AdminId}/permissions", admin.AccessToken, new { permissions = Array.Empty<string>() });

        Assert.Equal("SYSTEM_ROLE_LOCKED", await deleteCustomer.ErrorCodeAsync());
        Assert.Equal("ADMIN_ROLE_LOCKED", await editAdmin.ErrorCodeAsync());
    }

    [Fact]
    public async Task CreateRole_ShouldValidateNameAndPermissions()
    {
        var admin = await _client.LoginAdminAsync();

        var unknown = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/roles", admin.AccessToken,
            new { name = "Valido", permissions = new[] { "nao.existe" } });
        var badName = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/roles", admin.AccessToken,
            new { name = "1 inválido", permissions = Array.Empty<string>() });
        var duplicate = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/roles", admin.AccessToken,
            new { name = "manager", permissions = Array.Empty<string>() });

        Assert.True((await unknown.ReadJsonAsync()).GetProperty("errors").TryGetProperty("permissions", out _));
        Assert.True((await badName.ReadJsonAsync()).GetProperty("errors").TryGetProperty("name", out _));
        Assert.Equal("ROLE_ALREADY_EXISTS", await duplicate.ErrorCodeAsync());
    }
}
