using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace LittleEcommerce.Source.Shared.Serialization;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Apply(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
