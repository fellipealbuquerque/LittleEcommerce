namespace LittleEcommerce.Source.Application.Abstractions;

public record AccessToken(string Value, string TokenId, DateTime ExpiresAt);

public record IssuedRefreshToken(string Value, string Hash, DateTime ExpiresAt);

public record ClientInfo(string? IpAddress, string? UserAgent);
