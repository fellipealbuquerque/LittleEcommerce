using System.Globalization;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Mongo;
using MongoDB.Bson;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LittleEcommerce.Source.Infrastructure.Logging;

public static class MongoLogDocumentFactory
{
    private const int MaxDepth = 10;
    private const int MaxExceptionDepth = 5;

    private static readonly MessageTemplateTextFormatter MessageFormatter = new("{Message:lj}", CultureInfo.InvariantCulture);

    private static readonly Dictionary<string, string> PromotedProperties = new()
    {
        [LogProperties.CorrelationId] = "correlationId",
        [LogProperties.SourceContext] = "sourceContext",
        [LogProperties.UserId] = "userId",
        [LogProperties.RequestPath] = "requestPath",
        [LogProperties.StatusCode] = "statusCode"
    };

    public static BsonDocument Create(LogEvent logEvent)
    {
        var document = new BsonDocument
        {
            ["timestamp"] = new BsonDateTime(logEvent.Timestamp.UtcDateTime),
            ["level"] = logEvent.Level.ToString(),
            ["message"] = Render(logEvent),
            ["template"] = logEvent.MessageTemplate.Text
        };

        if (logEvent.TraceId is { } traceId)
            document["traceId"] = traceId.ToHexString();

        if (logEvent.SpanId is { } spanId)
            document["spanId"] = spanId.ToHexString();

        var properties = new BsonDocument();

        foreach (var (name, value) in logEvent.Properties)
        {
            if (PromotedProperties.TryGetValue(name, out var field))
            {
                var promoted = ToBson(value, 0);
                if (!promoted.IsBsonNull)
                    document[field] = promoted;
                continue;
            }

            properties[SafeKey(name)] = ToBson(value, 0);
        }

        if (logEvent.Exception is not null)
            document["exception"] = ToBson(logEvent.Exception, 0);

        document["properties"] = properties;

        return document;
    }

    private static string Render(LogEvent logEvent)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        MessageFormatter.Format(logEvent, writer);
        return writer.ToString();
    }

    internal static string SafeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "_";

        var safe = key.Replace('.', '_');
        return safe[0] == '$' ? "_" + safe[1..] : safe;
    }

    private static BsonValue ToBson(LogEventPropertyValue value, int depth)
    {
        if (depth > MaxDepth)
            return new BsonString("...");

        return value switch
        {
            ScalarValue scalar => BsonValues.From(scalar.Value),
            SequenceValue sequence => new BsonArray(sequence.Elements.Select(e => ToBson(e, depth + 1))),
            StructureValue structure => ToBson(structure, depth),
            DictionaryValue dictionary => ToBson(dictionary, depth),
            _ => new BsonString(value.ToString())
        };
    }

    private static BsonDocument ToBson(StructureValue structure, int depth)
    {
        var document = new BsonDocument();

        if (structure.TypeTag is not null)
            document["_type"] = structure.TypeTag;

        foreach (var property in structure.Properties)
            document[SafeKey(property.Name)] = ToBson(property.Value, depth + 1);

        return document;
    }

    private static BsonDocument ToBson(DictionaryValue dictionary, int depth)
    {
        var document = new BsonDocument();

        foreach (var (key, value) in dictionary.Elements)
            document[SafeKey(Convert.ToString(key.Value, CultureInfo.InvariantCulture) ?? string.Empty)] = ToBson(value, depth + 1);

        return document;
    }

    private static BsonDocument ToBson(Exception exception, int depth)
    {
        var document = new BsonDocument
        {
            ["type"] = exception.GetType().FullName ?? exception.GetType().Name,
            ["message"] = exception.Message
        };

        if (exception.StackTrace is not null)
            document["stackTrace"] = exception.StackTrace;

        if (exception.InnerException is not null && depth < MaxExceptionDepth)
            document["inner"] = ToBson(exception.InnerException, depth + 1);

        return document;
    }
}
