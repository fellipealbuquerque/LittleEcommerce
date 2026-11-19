namespace LittleEcommerce.Source.Common.Errors;

public static class ErrorCodes
{
    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserBlocked = "USER_BLOCKED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string RefreshTokenReused = "REFRESH_TOKEN_REUSED";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string AuthUnavailable = "AUTH_UNAVAILABLE";
    public const string RecordAlreadyExists = "RECORD_ALREADY_EXISTS";
    public const string RecordInUse = "RECORD_IN_USE";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
}
