using System.Text.Json;
using LittleEcommerce.Source.Shared.Helpers;
using LittleEcommerce.Source.Shared.Serialization;
using MongoDB.Bson;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public static class AuditSanitizer
{
    public static BsonDocument? Arguments(IDictionary<string, object?> arguments)
    {
        var relevant = arguments
            .Where(a => a.Value is not CancellationToken)
            .ToDictionary(a => a.Key, a => a.Value);

        if (relevant.Count == 0)
            return null;

        var document = BsonDocument.Parse(JsonSerializer.Serialize(relevant, JsonDefaults.Options));
        return Mask(document);
    }

    public static BsonDocument Mask(BsonDocument document)
    {
        foreach (var element in document.ToList())
            document[element.Name] = SensitiveData.IsSensitive(element.Name) ? SensitiveData.Mask : Mask(element.Value);

        return document;
    }

    private static BsonValue Mask(BsonValue value) => value switch
    {
        BsonDocument nested => Mask(nested),
        BsonArray array => new BsonArray(array.Select(Mask)),
        _ => value
    };
}
