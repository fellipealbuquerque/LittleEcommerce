using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;

namespace LittleEcommerce.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class AuthFlowTests(IntegrationFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task Register_ShouldCreateCustomerAndReturnWorkingTokens()
    {
        var email = AuthApi.NewEmail();

        var response = await _client.PostRegisterAsync(email);

        var data = (await response.ReadJsonAsync()).GetProperty("data");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Bearer", data.GetProperty("tokenType").GetString());
        Assert.Equal("Customer", data.GetProperty("user").GetProperty("role").GetString());
        Assert.Equal("Active", data.GetProperty("user").GetProperty("status").GetString());

        var me = await _client.GetMeAsync(data.GetProperty("accessToken").GetString()!);
        var profile = (await me.ReadJsonAsync()).GetProperty("data");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(email, profile.GetProperty("email").GetString());
        Assert.True(profile.GetProperty("createdAt").GetDateTime() > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Register_ShouldRejectDuplicateEmailIgnoringCase()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        var response = await _client.PostRegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Register_ShouldReturnFieldErrorsWithJsonNames()
    {
        var response = await _client.PostRegisterAsync("nao-e-email", "fraca", "A");

        var body = await response.ReadJsonAsync();
        var errors = body.GetProperty("errors");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
        Assert.True(errors.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task Register_ShouldReportMalformedJsonInPortuguese()
    {
        using var content = new StringContent("{\"name\":\"A\"", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/auth/register", content);

        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("JSON inválido ou com valor de tipo incorreto", errors.GetProperty("body")[0].GetString());
        Assert.False(errors.TryGetProperty("request", out _));
    }

    [Fact]
    public async Task Login_ShouldWorkForSeededAdmin()
    {
        var response = await _client.PostLoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var user = (await response.ReadJsonAsync()).GetProperty("data").GetProperty("user");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Admin", user.GetProperty("role").GetString());
        Assert.True(user.TryGetProperty("lastLoginAt", out _));
    }

    [Fact]
    public async Task Login_ShouldUseSameErrorForWrongPasswordAndUnknownEmail()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        var wrongPassword = await _client.PostLoginAsync(email, "OutraSenha999");
        var unknownEmail = await _client.PostLoginAsync(AuthApi.NewEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await wrongPassword.ErrorCodeAsync());
        Assert.Equal("INVALID_CREDENTIALS", await unknownEmail.ErrorCodeAsync());
    }

    [Fact]
    public async Task ProtectedRoute_ShouldReturnEnvelope_WhenTokenIsMissingOrGarbage()
    {
        var missing = await _client.GetAsync("/api/v1/users/me");
        var garbage = await _client.GetMeAsync("nao-e-um-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal("UNAUTHORIZED", await missing.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, garbage.StatusCode);
        Assert.Equal("UNAUTHORIZED", await garbage.ErrorCodeAsync());
    }
}
