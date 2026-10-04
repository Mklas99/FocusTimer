namespace FocusTimer.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>Reads project rules without letting one malformed rule invalidate unrelated settings.</summary>
    public sealed class ProjectRuleListConverter : JsonConverter<List<ProjectRule>>
    {
        /// <inheritdoc/>
        public override List<ProjectRule> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var rules = new List<ProjectRule>();
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

                var rule = new ProjectRule(
                    ReadText(element, "appPattern"),
                    ReadText(element, "titlePattern"),
                    ReadText(element, "projectName")?.Trim());
                if (rule.IsValid)
                {
                    rules.Add(rule);
                }
            }

            return rules;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, List<ProjectRule> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var rule in value)
            {
                writer.WriteStartObject();
                writer.WriteString("appPattern", rule.AppPattern);
                writer.WriteString("titlePattern", rule.TitlePattern);
                writer.WriteString("projectName", rule.ProjectName);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static string? ReadText(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
                }
            }

            return null;
        }
    }
}
