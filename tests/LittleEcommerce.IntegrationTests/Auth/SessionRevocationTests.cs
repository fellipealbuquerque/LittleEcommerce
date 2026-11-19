using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Infrastructure.Security;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Time.Testing;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace LittleEcommerce.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class SessionRevocationTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task Refresh_ShouldRotateTokens()
    {
        var first = await _client.RegisterAsync();

        var second = await _client.RefreshAsync(first);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetMeAsync(second.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldEndEverySession_WhenRotatedTokenIsReused()
    {
        var first = await _client.RegisterAsync();
        var second = await _client.RefreshAsync(first);

        var replay = await _client.PostRefreshAsync(first.RefreshToken);
        var afterReplay = await _client.PostRefreshAsync(second.RefreshToken);
        var me = await _client.GetMeAsync(second.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("REFRESH_TOKEN_REUSED", await replay.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await afterReplay.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("SESSION_REVOKED", await me.ErrorCodeAsync());
    }

    [Fact]
    public async Task Refresh_ShouldRejectUnknownToken()
    {
        var response = await _client.PostRefreshAsync("token-que-nunca-existiu");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Logout_ShouldBlacklistAccessTokenAndEndRefreshToken()
    {
        var session = await _client.RegisterAsync();

        var logout = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/auth/logout", session.AccessToken, new { refreshToken = session.RefreshToken });
        var me = await _client.GetMeAsync(session.AccessToken);
        var refresh = await _client.PostRefreshAsync(session.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("TOKEN_REVOKED", await me.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await refresh.ErrorCodeAsync());
    }

    [Fact]
    public async Task Logout_ShouldOnlyAffectCurrentSession()
    {
        var email = AuthApi.NewEmail();
        var phone = await _client.RegisterAsync(email);
        var laptop = await _client.LoginAsync(email);

        await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/auth/logout", phone.AccessToken, new { refreshToken = phone.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, (await _client.GetMeAsync(laptop.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostRefreshAsync(laptop.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task LogoutAll_ShouldEndEverySessionOfTheUser()
    {
        var email = AuthApi.NewEmail();
        var phone = await _client.RegisterAsync(email);
        var laptop = await _client.LoginAsync(email);

        var response = await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/auth/logout-all", phone.AccessToken);
        var laptopMe = await _client.GetMeAsync(laptop.AccessToken);
        var laptopRefresh = await _client.PostRefreshAsync(laptop.RefreshToken);
        var fresh = await _client.LoginAsync(email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SESSION_REVOKED", await laptopMe.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, laptopRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetMeAsync(fresh.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task RevokedToken_ShouldBeTreatedAsAnonymousOnPublicRoutes()
    {
        var session = await _client.RegisterAsync();
        await _client.SendAuthorizedAsync(HttpMethod.Post, "/api/v1/auth/logout", session.AccessToken);

        var response = await _client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/system/version", session.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_ShouldReturnTokenExpired()
    {
        var session = await _client.RegisterAsync();
        var past = new FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(-2));
        var service = new TokenService(MsOptions.Create(new JwtOptions
        {
            Issuer = ApiFactory.JwtIssuer,
            Audience = ApiFactory.JwtAudience,
            Secret = ApiFactory.JwtSecret,
            AccessTokenMinutes = 15
        }), past);
        var expired = service.CreateAccessToken(new User
        {
            Id = Guid.Parse(session.UserId),
            Email = session.Email,
            Name = "Cliente Teste",
            Role = new Role { Name = Roles.Customer }
        });

        var response = await _client.GetMeAsync(expired.Value);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("TOKEN_EXPIRED", await response.ErrorCodeAsync());
        Assert.Contains("invalid_token", response.Headers.WwwAuthenticate.ToString());
    }
}
