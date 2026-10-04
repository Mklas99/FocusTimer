namespace FocusTimer.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>Reads a rule list without letting one malformed rule invalidate unrelated settings.</summary>
    public sealed class WindowMatchRuleListConverter : JsonConverter<List<WindowMatchRule>>
    {
        /// <inheritdoc/>
        public override List<WindowMatchRule> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var rules = new List<WindowMatchRule>();
            using var value = JsonDocument.ParseValue(ref reader);
            if (value.RootElement.ValueKind != JsonValueKind.Array)
            {
                return rules;
            }

            foreach (var element in value.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var rule = new WindowMatchRule(ReadText(element, "appPattern"), ReadText(element, "titlePattern"));
                if (rule.IsValid)
                {
                    rules.Add(rule);
                }
            }

            return rules;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, List<WindowMatchRule> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var rule in value)
            {
                writer.WriteStartObject();
                writer.WriteString("appPattern", rule.AppPattern);
                writer.WriteString("titlePattern", rule.TitlePattern);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static string? ReadText(JsonElement element, string name)
        {
            // A missing property yields the default JsonProperty, whose value has kind Undefined.
            var property = element.EnumerateObject()
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
        }
    }
}
