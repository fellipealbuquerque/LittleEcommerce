using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Security;
using LittleEcommerce.Source.Shared.Options;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace LittleEcommerce.UnitTests.Security;

public class TokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Settings = new()
    {
        Issuer = "little-ecommerce",
        Audience = "clients",
        Secret = new string('s', 40),
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private static TokenService Create() => new(MsOptions.Create(Settings), new FakeTimeProvider(Now));

    [Fact]
    public void CreateAccessToken_ShouldCarryIdentityRoleAndSessionVersion()
    {
        var user = TestUsers.Create(sessionVersion: 3);

        var token = Create().CreateAccessToken(user);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.Value);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(token.TokenId, jwt.Id);
        Assert.Equal(Roles.Customer, jwt.GetClaim(AppClaims.Role).Value);
        Assert.Equal("3", jwt.GetClaim(AppClaims.SessionVersion).Value);
        Assert.Equal("little-ecommerce", jwt.Issuer);
        Assert.Equal(Now.UtcDateTime.AddMinutes(15), token.ExpiresAt);
        Assert.Equal(token.ExpiresAt, jwt.ValidTo);
    }

    [Fact]
    public async Task CreateAccessToken_ShouldBeSignedWithConfiguredSecret()
    {
        var token = Create().CreateAccessToken(TestUsers.Create());

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = JwtKeys.SigningKey(Settings.Secret),
            ValidateLifetime = false
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateAccessToken_ShouldUseUniqueTokenIds()
    {
        var service = Create();
        var user = TestUsers.Create();

        Assert.NotEqual(service.CreateAccessToken(user).TokenId, service.CreateAccessToken(user).TokenId);
    }

    [Fact]
    public void CreateRefreshToken_ShouldReturnRandomValueWithStableHash()
    {
        var service = Create();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.True(first.Value.Length >= 80);
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(first.Hash, service.HashRefreshToken(first.Value));
        Assert.DoesNotContain(first.Value, first.Hash);
        Assert.Equal(Now.UtcDateTime.AddDays(7), first.ExpiresAt);
    }
}
