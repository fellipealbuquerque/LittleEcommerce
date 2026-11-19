using LittleEcommerce.Source.Infrastructure.Logging;
using MongoDB.Bson;
using Serilog.Events;
using static LittleEcommerce.UnitTests.Logging.LogEventBuilder;

namespace LittleEcommerce.UnitTests.Logging;

public class MongoLogDocumentFactoryTests
{
    [Fact]
    public void Create_ShouldMapCoreFieldsAndPromoteKnownProperties()
    {
        var timestamp = new DateTimeOffset(2026, 11, 19, 15, 30, 0, TimeSpan.FromHours(-3));
        var logEvent = Create("Pedido {OrderId} total {Total}",
        [
            Property("OrderId", 42),
            Property("Total", 199.90m),
            Property("CorrelationId", "abc-123"),
            Property("SourceContext", "LittleEcommerce.Checkout"),
            Property("UserId", null)
        ], LogEventLevel.Warning, timestamp: timestamp);

        var document = MongoLogDocumentFactory.Create(logEvent);

        Assert.Equal(timestamp.UtcDateTime, document["timestamp"].ToUniversalTime());
        Assert.Equal("Warning", document["level"].AsString);
        Assert.Equal("Pedido {OrderId} total {Total}", document["template"].AsString);
        Assert.Contains("42", document["message"].AsString);
        Assert.Equal("abc-123", document["correlationId"].AsString);
        Assert.Equal("LittleEcommerce.Checkout", document["sourceContext"].AsString);
        Assert.False(document.Contains("userId"));

        var properties = document["properties"].AsBsonDocument;
        Assert.Equal(42, properties["OrderId"].AsInt32);
        Assert.Equal(199.90m, properties["Total"].AsDecimal);
        Assert.False(properties.Contains("CorrelationId"));
    }

    [Fact]
    public void Create_ShouldRenderStringsWithoutQuotes()
    {
        var logEvent = Create("HTTP {RequestMethod} {RequestPath}",
        [
            Property("RequestMethod", "GET"),
            Property("RequestPath", "/api/v1/products")
        ]);

        Assert.Equal("HTTP GET /api/v1/products", MongoLogDocumentFactory.Create(logEvent)["message"].AsString);
    }

    [Fact]
    public void Create_ShouldSanitizeKeysThatMongoRejects()
    {
        var logEvent = Create("Erros {Errors}",
        [
            new LogEventProperty("Errors", new DictionaryValue(
            [
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("items[0].quantity"), new ScalarValue("inválido")),
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("$where"), new ScalarValue("x"))
            ]))
        ]);

        var errors = MongoLogDocumentFactory.Create(logEvent)["properties"]["Errors"].AsBsonDocument;

        Assert.Equal("inválido", errors["items[0]_quantity"].AsString);
        Assert.Equal("x", errors["_where"].AsString);
    }

    [Fact]
    public void Create_ShouldStoreExceptionWithInnerChain()
    {
        var exception = new InvalidOperationException("falhou", new TimeoutException("sem resposta"));
        var logEvent = Create("Erro", [], LogEventLevel.Error, exception);

        var stored = MongoLogDocumentFactory.Create(logEvent)["exception"].AsBsonDocument;

        Assert.Equal(typeof(InvalidOperationException).FullName, stored["type"].AsString);
        Assert.Equal("falhou", stored["message"].AsString);
        Assert.Equal("sem resposta", stored["inner"]["message"].AsString);
    }

    [Fact]
    public void Create_ShouldConvertSequencesAndStructures()
    {
        var logEvent = Create("Itens {Items}",
        [
            new LogEventProperty("Items", new SequenceValue(
            [
                new StructureValue([Property("Sku", "A1"), Property("Qty", 2)], "CartItem")
            ]))
        ]);

        var item = MongoLogDocumentFactory.Create(logEvent)["properties"]["Items"].AsBsonArray[0].AsBsonDocument;

        Assert.Equal("CartItem", item["_type"].AsString);
        Assert.Equal("A1", item["Sku"].AsString);
        Assert.Equal(2, item["Qty"].AsInt32);
    }

    [Theory]
    [InlineData("a.b", "a_b")]
    [InlineData("$set", "_set")]
    [InlineData("", "_")]
    [InlineData("normal", "normal")]
    public void SafeKey_ShouldReplaceForbiddenCharacters(string key, string expected)
    {
        Assert.Equal(expected, MongoLogDocumentFactory.SafeKey(key));
    }
}
