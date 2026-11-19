using System.Net;
using System.Text.Json;
using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Common.Constants;

namespace LittleEcommerce.IntegrationTests.Admin;

[Collection(IntegrationCollection.Name)]
public class AuditTrailTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    private static string? Text(JsonElement element, params string[] path)
    {
        foreach (var segment in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(segment, out element))
                return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    [Fact]
    public async Task Register_ShouldRecordUserCreationWithMaskedPasswordHash()
    {
        var admin = await _client.LoginAdminAsync();
        var user = await _client.RegisterAsync();

        var entry = await _client.WaitForAuditAsync(admin, $"entityType=User&entityId={user.UserId}&kind=entity-change",
            e => Text(e, "action") == "Created");

        Assert.Equal(user.Email, Text(entry, "changes", "email", "new"));
        Assert.Equal("***", Text(entry, "changes", "passwordHash", "new"));
        Assert.Equal("anonymous", Text(entry, "actor", "type"));
    }

    [Fact]
    public async Task Block_ShouldRecordDataChangeAndAdminAction()
    {
        var admin = await _client.LoginAdminAsync();
        var user = await _client.RegisterAsync();

        await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{user.UserId}/block", admin.AccessToken);

        var change = await _client.WaitForAuditAsync(admin, $"entityType=User&entityId={user.UserId}&kind=entity-change",
            e => Text(e, "changes", "status", "new") == "Blocked");
        var action = await _client.WaitForAuditAsync(admin, $"kind=admin-action&action=AdminUsers.Block&actorId={admin.UserId}",
            e => Text(e, "request", "arguments", "id") == user.UserId);

        Assert.Equal("Active", Text(change, "changes", "status", "old"));
        Assert.Equal(admin.UserId, Text(change, "actor", "userId"));
        Assert.Equal(Permissions.UsersManage, Text(action, "permission"));
        Assert.Equal("200", Text(action, "statusCode"));
        Assert.Equal("True", Text(action, "succeeded"));
    }

    [Fact]
    public async Task FailedAdminAction_ShouldBeRecordedWithItsStatus()
    {
        var admin = await _client.LoginAdminAsync();

        await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{admin.UserId}/block", admin.AccessToken);

        var action = await _client.WaitForAuditAsync(admin, $"kind=admin-action&action=AdminUsers.Block&actorId={admin.UserId}",
            e => Text(e, "request", "arguments", "id") == admin.UserId);

        Assert.Equal("422", Text(action, "statusCode"));
        Assert.Equal("False", Text(action, "succeeded"));
    }

    [Fact]
    public async Task ForbiddenAttempt_ShouldBeRecordedAsAccessDenied()
    {
        var admin = await _client.LoginAdminAsync();
        var customer = await _client.RegisterAsync();

        var response = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/roles", customer.AccessToken);

        var entry = await _client.WaitForAuditAsync(admin, $"kind=access-denied&actorId={customer.UserId}", _ => true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(Permissions.RolesRead, Text(entry, "permission"));
        Assert.Equal("GET /api/v1/admin/roles", Text(entry, "action"));
    }

    [Fact]
    public async Task AuditLogs_ShouldRequireAuditPermission()
    {
        var admin = await _client.LoginAdminAsync();
        var manager = await _client.RegisterWithRoleAsync(admin, Roles.Manager);

        var response = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/audit-logs", manager.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
