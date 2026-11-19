using LittleEcommerce.Source.Infrastructure.Auditing;

namespace LittleEcommerce.UnitTests.Auditing;

public class AuditSanitizerTests
{
    private sealed record Payload(string Name, string NewPassword, IReadOnlyList<Item> Items);

    private sealed record Item(string Sku, string ApiKey);

    [Fact]
    public void Arguments_ShouldMaskSecretsAndSkipCancellationToken()
    {
        var id = Guid.NewGuid();
        var arguments = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["request"] = new Payload("Ana", "Segredo123", [new Item("A1", "k-1")]),
            ["ct"] = CancellationToken.None
        };

        var document = AuditSanitizer.Arguments(arguments)!;

        Assert.False(document.Contains("ct"));
        Assert.Equal(id.ToString(), document["id"].AsString);
        Assert.Equal("Ana", document["request"]["name"].AsString);
        Assert.Equal("***", document["request"]["newPassword"].AsString);
        Assert.Equal("A1", document["request"]["items"][0]["sku"].AsString);
        Assert.Equal("***", document["request"]["items"][0]["apiKey"].AsString);
    }

    [Fact]
    public void Arguments_ShouldReturnNull_WhenOnlyCancellationTokenIsPresent()
    {
        Assert.Null(AuditSanitizer.Arguments(new Dictionary<string, object?> { ["ct"] = CancellationToken.None }));
    }
}
