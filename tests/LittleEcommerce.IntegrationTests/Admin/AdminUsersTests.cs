using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;

namespace LittleEcommerce.IntegrationTests.Admin;

[Collection(IntegrationCollection.Name)]
public class AdminUsersTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task AdminRoutes_ShouldRequireAuthentication()
    {
        var response = await _client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_ShouldDocumentRequiredPermission()
    {
        var operation = (await (await _client.GetAsync("/openapi/v1.json")).ReadJsonAsync())
            .GetProperty("paths").GetProperty("/api/v1/admin/users/{id}/block").GetProperty("patch");

        Assert.Contains("`users.manage`", operation.GetProperty("description").GetString());
        Assert.True(operation.GetProperty("responses").TryGetProperty("403", out _));
    }

    [Fact]
    public async Task Customer_ShouldBeForbiddenWithEnvelope()
    {
        var customer = await _client.RegisterAsync();

        var response = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/users", customer.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Admin_ShouldSearchUsersWithPagination()
    {
        var admin = await _client.LoginAdminAsync();
        var target = await _client.RegisterAsync();

        var response = await _client.SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/admin/users?search={Uri.EscapeDataString(target.Email)}&limit=5", admin.AccessToken);

        var body = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        Assert.Equal(5, body.GetProperty("limit").GetInt32());
        Assert.Equal(target.UserId, body.GetProperty("data")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Block_ShouldEndSessionsAndStopLogin_UntilUnblocked()
    {
        var admin = await _client.LoginAdminAsync();
        var target = await _client.RegisterAsync();

        var block = await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{target.UserId}/block", admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, block.StatusCode);
        Assert.Equal("SESSION_REVOKED", await (await _client.GetMeAsync(target.AccessToken)).ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostRefreshAsync(target.RefreshToken)).StatusCode);

        var blockedLogin = await _client.PostLoginAsync(target.Email);
        Assert.Equal(HttpStatusCode.Forbidden, blockedLogin.StatusCode);
        Assert.Equal("USER_BLOCKED", await blockedLogin.ErrorCodeAsync());

        await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{target.UserId}/unblock", admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await _client.PostLoginAsync(target.Email)).StatusCode);
    }

    [Fact]
    public async Task ChangeRole_ShouldApplyManagerPermissionsAfterNewLogin()
    {
        var admin = await _client.LoginAdminAsync();
        var email = AuthApi.NewEmail();
        var before = await _client.RegisterAsync(email);

        var change = await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{before.UserId}/role", admin.AccessToken, new { role = "Manager" });
        var oldToken = await _client.GetMeAsync(before.AccessToken);
        var manager = await _client.LoginAsync(email);
        var canRead = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/users", manager.AccessToken);
        var canBlock = await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{admin.UserId}/block", manager.AccessToken);

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal("SESSION_REVOKED", await oldToken.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.OK, canRead.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, canBlock.StatusCode);
    }

    [Fact]
    public async Task Admin_ShouldNotChangeOwnAccount()
    {
        var admin = await _client.LoginAdminAsync();

        var block = await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{admin.UserId}/block", admin.AccessToken);
        var role = await _client.SendAuthorizedAsync(HttpMethod.Patch, $"/api/v1/admin/users/{admin.UserId}/role", admin.AccessToken, new { role = "Customer" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, block.StatusCode);
        Assert.Equal("CANNOT_CHANGE_OWN_ACCOUNT", await block.ErrorCodeAsync());
        Assert.Equal("CANNOT_CHANGE_OWN_ACCOUNT", await role.ErrorCodeAsync());
    }

    [Fact]
    public async Task RevokeSessions_ShouldInvalidateTargetTokens()
    {
        var admin = await _client.LoginAdminAsync();
        var target = await _client.RegisterAsync();

        var response = await _client.SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/admin/users/{target.UserId}/revoke-sessions", admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SESSION_REVOKED", await (await _client.GetMeAsync(target.AccessToken)).ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.OK, (await _client.PostLoginAsync(target.Email)).StatusCode);
    }
}
