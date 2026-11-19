using System.Text.Json;

namespace LittleEcommerce.IntegrationTests.Infrastructure;

public static class MailpitApi
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(20);

    public static async Task<IReadOnlyList<JsonElement>> MessagesToAsync(this HttpClient mailpit, string address)
    {
        var query = Uri.EscapeDataString($"to:\"{address}\"");
        var body = await (await mailpit.GetAsync($"/api/v1/search?query={query}")).ReadJsonAsync();

        return body.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array
            ? messages.EnumerateArray().ToList()
            : [];
    }

    public static async Task<IReadOnlyList<JsonElement>> WaitForMessagesAsync(this HttpClient mailpit, string address, int count)
    {
        var deadline = DateTime.UtcNow + WaitLimit;

        while (DateTime.UtcNow < deadline)
        {
            var messages = await mailpit.MessagesToAsync(address);

            if (messages.Count >= count)
                return messages;

            await Task.Delay(200);
        }

        throw new TimeoutException($"Esperava {count} e-mail(s) para {address} em {WaitLimit.TotalSeconds}s");
    }
}
