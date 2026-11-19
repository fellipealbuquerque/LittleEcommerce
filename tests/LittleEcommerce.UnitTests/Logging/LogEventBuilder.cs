using Serilog.Events;
using Serilog.Parsing;

namespace LittleEcommerce.UnitTests.Logging;

internal static class LogEventBuilder
{
    private static readonly MessageTemplateParser Parser = new();

    public static LogEvent Create(
        string template,
        IEnumerable<LogEventProperty> properties,
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null,
        DateTimeOffset? timestamp = null) =>
        new(timestamp ?? DateTimeOffset.UtcNow, level, exception, Parser.Parse(template), properties);

    public static LogEventProperty Property(string name, object? value) => new(name, new ScalarValue(value));

    public static LogEventProperty Structure(string name, params LogEventProperty[] properties) =>
        new(name, new StructureValue(properties));
}
