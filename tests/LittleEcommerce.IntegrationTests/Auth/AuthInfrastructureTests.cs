using System.Net;
using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Data;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Infrastructure.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LittleEcommerce.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class AuthInfrastructureTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Login_ShouldBeRateLimitedPerClient()
    {
        using var limited = fixture.Factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:AuthPermitLimit", "2"));
        using var client = limited.CreateClient();
        var email = AuthApi.NewEmail();

        await client.PostLoginAsync(email);
        await client.PostLoginAsync(email);
        var blocked = await client.PostLoginAsync(email);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("TOO_MANY_REQUESTS", await blocked.ErrorCodeAsync());
        Assert.True(blocked.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task CleanupWorker_ShouldDeleteOnlyExpiredRefreshTokens()
    {
        var session = await fixture.Factory.CreateClient().RegisterAsync();
        var userId = Guid.Parse(session.UserId);
        var expiredId = Guid.CreateVersion7();

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.RefreshTokens.Add(new RefreshToken
            {
                Id = expiredId,
                UserId = userId,
                FamilyId = Guid.CreateVersion7(),
                TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ExpiresAt = DateTime.UtcNow.AddDays(-3)
            });
            await context.SaveChangesAsync();
        }

        var worker = fixture.Factory.Services.GetServices<IHostedService>().OfType<RefreshTokenCleanupWorker>().Single();
        var removed = await worker.CleanupAsync(CancellationToken.None);

        await using var check = fixture.Factory.Services.CreateAsyncScope();
        var tokens = await check.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .RefreshTokens.Where(t => t.UserId == userId).ToListAsync();

        Assert.True(removed >= 1);
        Assert.DoesNotContain(tokens, t => t.Id == expiredId);
        Assert.Single(tokens);
    }
}
