using System.Threading.RateLimiting;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Infrastructure.Security;
using LittleEcommerce.Source.Infrastructure.Workers;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class AuthExtensions
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(o => o.Secret.Length >= JwtOptions.MinimumSecretLength,
                $"Jwt:Secret precisa ter pelo menos {JwtOptions.MinimumSecretLength} caracteres. Defina Jwt__Secret.")
            .Validate(o => o.AccessTokenMinutes > 0 && o.RefreshTokenDays > 0,
                "Jwt:AccessTokenMinutes e Jwt:RefreshTokenDays precisam ser positivos.")
            .ValidateOnStart();

        services.Configure<PasswordHashingOptions>(configuration.GetSection(PasswordHashingOptions.Section));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = JwtKeys.SigningKey(jwt.Secret),
                    ClockSkew = ClockSkew,
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Role
                };
                bearer.Events = new JwtBearerEvents { OnChallenge = WriteChallengeAsync };
            });

        services.AddAuthorization();
        services.AddAuthRateLimiting(configuration);

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<ITokenRevocationStore, RedisTokenRevocationStore>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddHostedService<RefreshTokenCleanupWorker>();

        return services;
    }

    private static void AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitingOptions.Section).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(RateLimitingOptions.AuthPolicy, http =>
                RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, limits.AuthPermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, limits.AuthWindowSeconds)),
                        QueueLimit = 0
                    }));
        });
    }

    private static Task WriteChallengeAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var expired = context.AuthenticateFailure is SecurityTokenExpiredException;

        var error = expired
            ? APIErrors.Create(StatusCodes.Status401Unauthorized, "Token expirado. Renove com o refresh token.", context.HttpContext.TraceIdentifier, ErrorCodes.TokenExpired)
            : APIErrors.Create(StatusCodes.Status401Unauthorized, "Token ausente ou inválido", context.HttpContext.TraceIdentifier);

        context.Response.Headers.WWWAuthenticate = expired
            ? "Bearer error=\"invalid_token\", error_description=\"The token expired\""
            : "Bearer";

        return context.Response.WriteErrorAsync(error);
    }
}
