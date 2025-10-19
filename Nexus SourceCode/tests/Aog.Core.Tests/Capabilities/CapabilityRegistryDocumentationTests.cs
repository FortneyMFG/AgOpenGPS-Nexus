using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Aog.Core.Capabilities;
using Xunit;

namespace Aog.Core.Tests.Capabilities;

public sealed class CapabilityRegistryDocumentationTests
{
    [Fact]
    public void CapabilityRegistryEntriesRequireDocumentationFreeze()
    {
        var repoRoot = FindRepositoryRoot();
        var documentationPath = Path.Combine(repoRoot, "docs", "Core", "capability-registry.md");
        Assert.True(File.Exists(documentationPath), $"Capability registry reference not found at '{documentationPath}'.");

        var documentedCapabilities = ParseDocumentedCapabilities(documentationPath);
        var registeredCapabilities = new HashSet<string>(
            CapabilityRegistry.All.Select(definition => definition.Name),
            StringComparer.OrdinalIgnoreCase);

        var missing = registeredCapabilities
            .Where(name => !documentedCapabilities.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"Missing capability documentation entries for: {string.Join(", ", missing)}. Update docs/Core/capability-registry.md under the contracts freeze process before merging.");
    }

    private static ISet<string> ParseDocumentedCapabilities(string documentationPath)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowPattern = new Regex(@"^\|\s*`(?<name>[^`]+)`\s*\|", RegexOptions.Compiled);

        foreach (var line in File.ReadLines(documentationPath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("|", StringComparison.Ordinal) || trimmed.StartsWith("| ---"))
            {
                continue;
            }

            var match = rowPattern.Match(trimmed);
            if (match.Success)
            {
                names.Add(match.Groups["name"].Value.Trim());
            }
        }

        return names;
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "tasks.md")) && Directory.Exists(Path.Combine(current, "docs")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Unable to locate the repository root from the test context.");
    }
}
