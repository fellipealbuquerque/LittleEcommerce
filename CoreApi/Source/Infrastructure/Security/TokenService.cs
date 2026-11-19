using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LittleEcommerce.Source.Infrastructure.Security;

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private const int RefreshTokenBytes = 64;

    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(User user)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(jwt.AccessTokenMinutes);
        var tokenId = Guid.CreateVersion7().ToString("N");

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(JwtKeys.SigningKey(jwt.Secret), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [AppClaims.Subject] = user.Id.ToString(),
                [AppClaims.TokenId] = tokenId,
                [AppClaims.Email] = user.Email,
                [AppClaims.Name] = user.Name,
                [AppClaims.Role] = user.Role.Name,
                [AppClaims.SessionVersion] = user.SessionVersion
            }
        };

        return new AccessToken(_handler.CreateToken(descriptor), tokenId, expiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var expiresAt = clock.GetUtcNow().UtcDateTime.AddDays(options.Value.RefreshTokenDays);

        return new IssuedRefreshToken(value, HashRefreshToken(value), expiresAt);
    }

    public string HashRefreshToken(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
