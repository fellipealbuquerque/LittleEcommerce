using LittleEcommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LittleEcommerce.IntegrationTests;

public class ApiFactory(IntegrationFixture fixture) : WebApplicationFactory<Program>
{
    public const string JwtIssuer = "little-ecommerce";
    public const string JwtAudience = "little-ecommerce-clients";
    public const string JwtSecret = "integration-tests-only-jwt-secret-0123456789";
    public const string AdminEmail = "admin@tests.local";
    public const string AdminPassword = "AdminTests2026";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:PostgreSQL", fixture.PostgresConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", fixture.RedisConnectionString);
        builder.UseSetting("ConnectionStrings:Mongo", fixture.MongoConnectionString);
        builder.UseSetting("MongoLogging:Enabled", "false");
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:Secret", JwtSecret);
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("PasswordHashing:WorkFactor", "4");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
    }
}
