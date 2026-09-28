namespace FocusTimer.Core.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>Reads the developer interval without invalidating unrelated settings.</summary>
    public sealed class ActivityPollingIntervalConverter : JsonConverter<int>
    {
        /// <inheritdoc/>
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var value = JsonDocument.ParseValue(ref reader);
            return value.RootElement.ValueKind == JsonValueKind.Number &&
                value.RootElement.TryGetInt32(out var seconds) && seconds is >= 1 and <= 60 ? seconds : 10;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
