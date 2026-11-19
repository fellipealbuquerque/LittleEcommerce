using LittleEcommerce.Source.Infrastructure.Logging;
using Moq;
using Serilog.Core;
using Serilog.Events;
using static LittleEcommerce.UnitTests.Logging.LogEventBuilder;

namespace LittleEcommerce.UnitTests.Logging;

public class SensitiveDataMaskingEnricherTests
{
    private static readonly ILogEventPropertyFactory Factory = Mock.Of<ILogEventPropertyFactory>();

    private static object? ScalarOf(LogEventPropertyValue value) => ((ScalarValue)value).Value;

    [Fact]
    public void Enrich_ShouldMaskTopLevelSensitiveProperty_AndRenderedMessage()
    {
        var logEvent = Create("Login de {Email} com {Password}",
        [
            Property("Email", "ana@loja.com"),
            Property("Password", "segredo123")
        ]);

        new SensitiveDataMaskingEnricher().Enrich(logEvent, Factory);

        Assert.Equal("***", ScalarOf(logEvent.Properties["Password"]));
        Assert.Equal("ana@loja.com", ScalarOf(logEvent.Properties["Email"]));
        Assert.DoesNotContain("segredo123", logEvent.RenderMessage());
    }

    [Fact]
    public void Enrich_ShouldMaskNestedStructuresDictionariesAndSequences()
    {
        var headers = new DictionaryValue(
        [
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("Authorization"), new ScalarValue("Bearer abc")),
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("Accept"), new ScalarValue("application/json"))
        ]);

        var items = new SequenceValue([new StructureValue([Property("ApiKey", "k-1"), Property("Name", "item")])]);

        var logEvent = Create("Payload {User} {Headers} {Items}",
        [
            Structure("User", Property("Name", "Ana"), Property("refresh_token", "rt-1")),
            new LogEventProperty("Headers", headers),
            new LogEventProperty("Items", items)
        ]);

        new SensitiveDataMaskingEnricher().Enrich(logEvent, Factory);

        var user = (StructureValue)logEvent.Properties["User"];
        Assert.Equal("Ana", ScalarOf(user.Properties.Single(p => p.Name == "Name").Value));
        Assert.Equal("***", ScalarOf(user.Properties.Single(p => p.Name == "refresh_token").Value));

        var maskedHeaders = (DictionaryValue)logEvent.Properties["Headers"];
        Assert.Equal("***", ScalarOf(maskedHeaders.Elements.Single(e => Equals(e.Key.Value, "Authorization")).Value));
        Assert.Equal("application/json", ScalarOf(maskedHeaders.Elements.Single(e => Equals(e.Key.Value, "Accept")).Value));

        var maskedItem = (StructureValue)((SequenceValue)logEvent.Properties["Items"]).Elements[0];
        Assert.Equal("***", ScalarOf(maskedItem.Properties.Single(p => p.Name == "ApiKey").Value));
        Assert.Equal("item", ScalarOf(maskedItem.Properties.Single(p => p.Name == "Name").Value));
    }

    [Fact]
    public void Enrich_ShouldKeepSameInstances_WhenNothingIsSensitive()
    {
        var user = new StructureValue([Property("Name", "Ana")]);
        var logEvent = Create("Usuário {User}", [new LogEventProperty("User", user)]);

        new SensitiveDataMaskingEnricher().Enrich(logEvent, Factory);

        Assert.Same(user, logEvent.Properties["User"]);
    }
}
