using LittleEcommerce.Source.Shared.Helpers;
using Serilog.Core;
using Serilog.Events;

namespace LittleEcommerce.Source.Infrastructure.Logging;

public sealed class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    private static readonly ScalarValue Masked = new(SensitiveData.Mask);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var (name, value) in logEvent.Properties.ToList())
        {
            var masked = SensitiveData.IsSensitive(name) ? Masked : Mask(value);

            if (!ReferenceEquals(masked, value))
                logEvent.AddOrUpdateProperty(new LogEventProperty(name, masked));
        }
    }

    internal static LogEventPropertyValue Mask(LogEventPropertyValue value) => value switch
    {
        StructureValue structure => MaskStructure(structure),
        DictionaryValue dictionary => MaskDictionary(dictionary),
        SequenceValue sequence => MaskSequence(sequence),
        _ => value
    };

    private static LogEventPropertyValue MaskStructure(StructureValue structure)
    {
        var changed = false;
        var properties = new List<LogEventProperty>(structure.Properties.Count);

        foreach (var property in structure.Properties)
        {
            var masked = SensitiveData.IsSensitive(property.Name) ? Masked : Mask(property.Value);
            changed |= !ReferenceEquals(masked, property.Value);
            properties.Add(ReferenceEquals(masked, property.Value) ? property : new LogEventProperty(property.Name, masked));
        }

        return changed ? new StructureValue(properties, structure.TypeTag) : structure;
    }

    private static LogEventPropertyValue MaskDictionary(DictionaryValue dictionary)
    {
        var changed = false;
        var elements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(dictionary.Elements.Count);

        foreach (var (key, value) in dictionary.Elements)
        {
            var masked = SensitiveData.IsSensitive(key.Value?.ToString()) ? Masked : Mask(value);
            changed |= !ReferenceEquals(masked, value);
            elements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(key, masked));
        }

        return changed ? new DictionaryValue(elements) : dictionary;
    }

    private static LogEventPropertyValue MaskSequence(SequenceValue sequence)
    {
        var changed = false;
        var elements = new List<LogEventPropertyValue>(sequence.Elements.Count);

        foreach (var element in sequence.Elements)
        {
            var masked = Mask(element);
            changed |= !ReferenceEquals(masked, element);
            elements.Add(masked);
        }

        return changed ? new SequenceValue(elements) : sequence;
    }
}
