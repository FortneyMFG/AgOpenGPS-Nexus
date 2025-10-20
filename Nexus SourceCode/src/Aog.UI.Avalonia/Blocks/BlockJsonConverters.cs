using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.UI.Avalonia.Blocks;

/// <summary>
/// Provides JSON converters for strongly typed block identifiers.
/// </summary>
public static class BlockJsonConverters
{
    /// <summary>
    /// Adds the block identifier converters to the supplied serializer options.
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Converters.OfType<BlockDefinitionIdJsonConverter>().Any())
        {
            options.Converters.Add(new BlockDefinitionIdJsonConverter());
        }

        if (!options.Converters.OfType<BlockInstanceIdJsonConverter>().Any())
        {
            options.Converters.Add(new BlockInstanceIdJsonConverter());
        }
    }

    private sealed class BlockDefinitionIdJsonConverter : JsonConverter<BlockDefinitionId>
    {
        public override BlockDefinitionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new BlockDefinitionId(reader.GetString() ?? string.Empty),
                JsonTokenType.StartObject => ReadFromObject(ref reader),
                JsonTokenType.Null => new BlockDefinitionId(string.Empty),
                _ => throw new JsonException($"Unexpected token '{reader.TokenType}' when parsing BlockDefinitionId."),
            };
        }

        public override void Write(Utf8JsonWriter writer, BlockDefinitionId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.Value ?? string.Empty);
        }

        private static BlockDefinitionId ReadFromObject(ref Utf8JsonReader reader)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            if (document.RootElement.TryGetProperty("Value", out var valueProperty)
                && valueProperty.ValueKind == JsonValueKind.String)
            {
                return new BlockDefinitionId(valueProperty.GetString() ?? string.Empty);
            }

            throw new JsonException("Expected a string 'Value' property when parsing BlockDefinitionId.");
        }
    }

    private sealed class BlockInstanceIdJsonConverter : JsonConverter<BlockInstanceId>
    {
        public override BlockInstanceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => Create(reader.GetString()),
                JsonTokenType.StartObject => ReadFromObject(ref reader),
                JsonTokenType.Null => new BlockInstanceId(),
                _ => throw new JsonException($"Unexpected token '{reader.TokenType}' when parsing BlockInstanceId."),
            };
        }

        public override void Write(Utf8JsonWriter writer, BlockInstanceId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.Value.ToString() ?? string.Empty);
        }

        private static BlockInstanceId Create(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new BlockInstanceId();
            }

            if (Guid.TryParse(text, out var guid))
            {
                return new BlockInstanceId(guid);
            }

            throw new JsonException($"The value '{text}' is not a valid GUID for BlockInstanceId.");
        }

        private static BlockInstanceId ReadFromObject(ref Utf8JsonReader reader)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            if (document.RootElement.TryGetProperty("Value", out var valueProperty)
                && valueProperty.ValueKind == JsonValueKind.String)
            {
                return Create(valueProperty.GetString());
            }

            throw new JsonException("Expected a string 'Value' property when parsing BlockInstanceId.");
        }
    }
}
