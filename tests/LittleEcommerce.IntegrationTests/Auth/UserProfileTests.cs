using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;

namespace LittleEcommerce.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class UserProfileTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task UpdateMe_ShouldChangeName()
    {
        var session = await _client.RegisterAsync();

        var response = await _client.SendAuthorizedAsync(HttpMethod.Put, "/api/v1/users/me", session.AccessToken, new { name = "  Novo Nome  " });

        var data = (await response.ReadJsonAsync()).GetProperty("data");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Novo Nome", data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ChangePassword_ShouldRequireNewLoginEverywhere()
    {
        var email = AuthApi.NewEmail();
        var session = await _client.RegisterAsync(email);

        var change = await _client.SendAuthorizedAsync(HttpMethod.Patch, "/api/v1/users/me/password", session.AccessToken,
            new { currentPassword = AuthApi.Password, newPassword = "NovaSenha2026" });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal("SESSION_REVOKED", await (await _client.GetMeAsync(session.AccessToken)).ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostRefreshAsync(session.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostLoginAsync(email)).StatusCode);

        var fresh = await _client.LoginAsync(email, "NovaSenha2026");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetMeAsync(fresh.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldRejectWrongCurrentPasswordWithoutEndingSession()
    {
        var session = await _client.RegisterAsync();

        var change = await _client.SendAuthorizedAsync(HttpMethod.Patch, "/api/v1/users/me/password", session.AccessToken,
            new { currentPassword = "SenhaErrada1", newPassword = "NovaSenha2026" });

        var body = await change.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("currentPassword", out _));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetMeAsync(session.AccessToken)).StatusCode);
    }
}
