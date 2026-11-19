using LittleEcommerce.Source.Shared.DTOs.Results.Users;

namespace LittleEcommerce.Source.Shared.DTOs.Results.Auth;

public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string TokenType,
    UserResult User
);
