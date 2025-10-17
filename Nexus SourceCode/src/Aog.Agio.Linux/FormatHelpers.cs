using System.Globalization;

namespace Aog.Agio.Linux;

internal static class FormatHelpers
{
    public static string FormatDouble(double? value, string format)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? "n/a";
}
