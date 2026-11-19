using System.Globalization;
using MongoDB.Bson;

namespace LittleEcommerce.Source.Infrastructure.Mongo;

public static class BsonValues
{
    public static BsonValue From(object? value) => value switch
    {
        null => BsonNull.Value,
        string s => new BsonString(s),
        bool b => BsonBoolean.Create(b),
        int i => new BsonInt32(i),
        long l => new BsonInt64(l),
        short or byte or sbyte or ushort => new BsonInt32(Convert.ToInt32(value, CultureInfo.InvariantCulture)),
        uint ui => new BsonInt64(ui),
        ulong ul when ul <= long.MaxValue => new BsonInt64((long)ul),
        float f => new BsonDouble(f),
        double d => new BsonDouble(d),
        decimal m => new BsonDecimal128(m),
        DateTime dt => new BsonDateTime(dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime()),
        DateTimeOffset dto => new BsonDateTime(dto.UtcDateTime),
        Guid g => new BsonString(g.ToString()),
        TimeSpan ts => new BsonString(ts.ToString("c", CultureInfo.InvariantCulture)),
        Enum e => new BsonString(e.ToString()),
        _ => new BsonString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
    };
}
