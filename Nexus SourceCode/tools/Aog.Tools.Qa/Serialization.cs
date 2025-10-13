using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aog.Tools.Qa;

internal static class Serialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
