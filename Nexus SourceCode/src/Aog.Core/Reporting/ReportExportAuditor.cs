using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Aog.Core.Reporting;

/// <summary>
/// Produces deterministic audit summaries for generated reports so callers can diff
/// export contents across runs.
/// </summary>
public static class ReportExportAuditor
{
    /// <summary>
    /// Builds a multi-line summary describing the supplied report generation result.
    /// </summary>
    /// <param name="result">Report generation result to summarise.</param>
    /// <returns>Deterministic text suitable for diffing in QA pipelines.</returns>
    public static string GenerateSummary(ReportGenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine($"Template: {result.Template.Name} ({result.Template.Id}) v{result.Template.Version}");
        builder.AppendLine($"Scope: {result.Scope.Kind} — {result.Scope.Identifier}");
        builder.AppendLine($"GeneratedAtUtc: {result.GeneratedAt.ToUniversalTime():O}");

        AppendOutputs(result, builder);
        AppendSections(result, builder);
        AppendMetadata(result, builder);

        return builder.ToString().TrimEnd();
    }

    private static void AppendOutputs(ReportGenerationResult result, StringBuilder builder)
    {
        builder.AppendLine("Outputs:");
        if (result.Artifacts.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        var requested = ParseRequestedFormats(result.Metadata);
        foreach (var group in result.Artifacts
            .GroupBy(artifact => artifact.Format, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var format = group.Key;
            var requestedFlag = requested.Contains(format) ? "requested" : "unrequested";
            builder.AppendLine(
                $"  - {format} ({requestedFlag}, {group.Count().ToString(CultureInfo.InvariantCulture)} artifact(s))");

            foreach (var artifact in group.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"    • {artifact.Name}");
            }
        }

        foreach (var missing in requested.Except(
                     result.Artifacts.Select(a => a.Format),
                     StringComparer.OrdinalIgnoreCase)
                 .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  - {missing} (requested, 0 artifact(s))");
        }
    }

    private static void AppendSections(ReportGenerationResult result, StringBuilder builder)
    {
        builder.AppendLine("Sections:");
        if (result.Sections.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var section in result.Sections
                     .OrderBy(section => section.SectionId, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  - {section.SectionId}: {section.Status}");
            if (!string.IsNullOrWhiteSpace(section.Message))
            {
                builder.AppendLine($"    message: {section.Message}");
            }

            if (section.Diagnostics.Count > 0)
            {
                foreach (var diagnostic in section.Diagnostics
                             .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"    diag[{diagnostic.Key}]={diagnostic.Value}");
                }
            }
        }
    }

    private static void AppendMetadata(ReportGenerationResult result, StringBuilder builder)
    {
        builder.AppendLine("Metadata:");
        if (result.Metadata.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var pair in result.Metadata
                     .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  {pair.Key} = {pair.Value}");
        }
    }

    private static HashSet<string> ParseRequestedFormats(IReadOnlyDictionary<string, string> metadata)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (!pair.Key.StartsWith("requestedOutput.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var format = pair.Key["requestedOutput.".Length..];
            if (string.Equals(format, "count", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(format) &&
                string.Equals(pair.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                set.Add(format);
            }
        }

        return set;
    }
}
