using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LittleEcommerce.IntegrationTests.Infrastructure;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LittleEcommerce.IntegrationTests.Outbox;

[Collection(IntegrationCollection.Name)]
public class OutboxTests(IntegrationFixture fixture)
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(25);

    private async Task<WebApplicationFactory<Program>> WithDispatcherAsync(params (string Key, string Value)[] settings)
    {
        await DiscardBacklogFromOtherTestsAsync();

        return fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Outbox:DispatcherEnabled", "true");
            builder.UseSetting("Outbox:PollIntervalSeconds", "1");

            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
        });
    }

    private async Task DiscardBacklogFromOtherTestsAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending || m.Status == OutboxStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Sent)
                .SetProperty(m => m.LastError, "descartada antes de um teste de outbox"));
    }

    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<JsonElement> OutboxOfAsync(HttpClient client, Session admin, string userId) =>
        (await (await client.SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/admin/outbox?aggregateId={userId}", admin.AccessToken)).ReadJsonAsync())
        .GetProperty("data");

    private static async Task<JsonElement> WaitForStatusAsync(HttpClient client, Session admin, string userId, string status)
    {
        var deadline = DateTime.UtcNow + WaitLimit;

        while (DateTime.UtcNow < deadline)
        {
            var messages = await OutboxOfAsync(client, admin, userId);

            foreach (var message in messages.EnumerateArray())
            {
                if (message.GetProperty("status").GetString() == status)
                    return message;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Mensagem do usuário {userId} não chegou a {status} em {WaitLimit.TotalSeconds}s");
    }

    [Fact]
    public async Task Register_ShouldWriteWelcomeEmailToOutboxInSameTransaction()
    {
        var client = fixture.Factory.CreateClient();
        var admin = await client.LoginAdminAsync();

        var user = await client.RegisterAsync();

        var message = (await OutboxOfAsync(client, admin, user.UserId)).EnumerateArray().Single();
        Assert.Equal("email.welcome", message.GetProperty("type").GetString());
        Assert.Equal("Pending", message.GetProperty("status").GetString());
        Assert.Equal(user.Email, message.GetProperty("payload").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_ShouldDeliverWelcomeEmailRightAway()
    {
        using var factory = await WithDispatcherAsync();
        var client = factory.CreateClient();
        var admin = await client.LoginAdminAsync();

        var user = await client.RegisterAsync();

        var emails = await fixture.Mailpit.WaitForMessagesAsync(user.Email, 1);
        var sent = await WaitForStatusAsync(client, admin, user.UserId, "Sent");
        var summary = (await (await client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/outbox/summary", admin.AccessToken)).ReadJsonAsync()).GetProperty("data");

        Assert.Equal("Boas-vindas à LittleEcommerce, Cliente!", emails[0].GetProperty("Subject").GetString());
        Assert.Equal(1, sent.GetProperty("attempts").GetInt32());
        Assert.True(sent.TryGetProperty("processedAt", out _));
        Assert.True(summary.GetProperty("sent").GetInt32() >= 1);
    }

    [Fact]
    public async Task FailingSmtp_ShouldRetryThenDeadLetter_AndAdminCanRequeue()
    {
        using var factory = await WithDispatcherAsync(
            ("Email:Smtp:Host", "127.0.0.1"),
            ("Email:Smtp:Port", ClosedPort().ToString()),
            ("Email:Smtp:TimeoutSeconds", "2"),
            ("Outbox:MaxAttempts", "2"),
            ("Outbox:BaseRetryDelaySeconds", "1"));
        var client = factory.CreateClient();
        var admin = await client.LoginAdminAsync();

        var user = await client.RegisterAsync();
        var dead = await WaitForStatusAsync(client, admin, user.UserId, "DeadLetter");

        var retry = await client.SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/admin/outbox/{dead.GetProperty("id").GetString()}/retry", admin.AccessToken);
        var requeued = (await retry.ReadJsonAsync()).GetProperty("data");

        Assert.Equal(2, dead.GetProperty("attempts").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(dead.GetProperty("lastError").GetString()));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal("Pending", requeued.GetProperty("status").GetString());
        Assert.Equal(0, requeued.GetProperty("attempts").GetInt32());
    }

    [Fact]
    public async Task ResendWelcomeEmail_ShouldDeliverAnotherCopy()
    {
        using var factory = await WithDispatcherAsync();
        var client = factory.CreateClient();
        var admin = await client.LoginAdminAsync();
        var user = await client.RegisterAsync();
        await fixture.Mailpit.WaitForMessagesAsync(user.Email, 1);

        var resend = await client.SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/admin/users/{user.UserId}/resend-welcome-email", admin.AccessToken);

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Equal(2, (await fixture.Mailpit.WaitForMessagesAsync(user.Email, 2)).Count);
    }

    [Fact]
    public async Task OutboxRoutes_ShouldRequirePermission()
    {
        var client = fixture.Factory.CreateClient();
        var customer = await client.RegisterAsync();

        var list = await client.SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/outbox", customer.AccessToken);
        var resend = await client.SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/admin/users/{customer.UserId}/resend-welcome-email", customer.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, resend.StatusCode);
    }
}
