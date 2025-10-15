using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Plugins;

namespace Aog.Tools.PluginCompliance;

internal sealed class PluginManifestComplianceChecker
{
    private readonly PluginManifestLoader _loader;

    public PluginManifestComplianceChecker()
    {
        _loader = new PluginManifestLoader();
    }

    public async Task<ManifestLintResult> LintAsync(
        string manifestRoot,
        string baselineRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestRoot))
        {
            throw new ArgumentException("Manifest root must be provided.", nameof(manifestRoot));
        }

        if (string.IsNullOrWhiteSpace(baselineRoot))
        {
            throw new ArgumentException("Baseline root must be provided.", nameof(baselineRoot));
        }

        if (!Directory.Exists(manifestRoot))
        {
            throw new DirectoryNotFoundException($"Manifest root '{manifestRoot}' was not found.");
        }

        if (!Directory.Exists(baselineRoot))
        {
            throw new DirectoryNotFoundException($"Baseline root '{baselineRoot}' was not found.");
        }

        var diagnostics = new List<ManifestDiagnostic>();

        var manifestFiles = Directory.EnumerateFiles(manifestRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var manifestPath in manifestFiles)
        {
            var relative = Path.GetRelativePath(manifestRoot, manifestPath);
            PluginManifest? manifest = null;

            try
            {
                await using var stream = File.OpenRead(manifestPath);
                manifest = await _loader.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException or ArgumentException)
            {
                diagnostics.Add(new ManifestDiagnostic(
                    manifestPath,
                    $"Failed to load manifest: {ex.Message}",
                    ManifestDiagnosticLevel.Error));
                continue;
            }

            var baselinePath = Path.Combine(baselineRoot, relative);
            if (!File.Exists(baselinePath))
            {
                diagnostics.Add(new ManifestDiagnostic(
                    manifestPath,
                    $"Missing baseline snapshot at '{baselinePath}'.",
                    ManifestDiagnosticLevel.Error));
                continue;
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            var baselineJson = await File.ReadAllTextAsync(baselinePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            string canonicalManifest;
            string canonicalBaseline;

            try
            {
                canonicalManifest = ManifestJsonUtilities.Canonicalize(manifestJson);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                diagnostics.Add(new ManifestDiagnostic(
                    manifestPath,
                    $"Manifest JSON was invalid: {ex.Message}",
                    ManifestDiagnosticLevel.Error));
                continue;
            }

            try
            {
                canonicalBaseline = ManifestJsonUtilities.Canonicalize(baselineJson);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                diagnostics.Add(new ManifestDiagnostic(
                    manifestPath,
                    $"Baseline JSON at '{baselinePath}' was invalid: {ex.Message}",
                    ManifestDiagnosticLevel.Error));
                continue;
            }

            if (!string.Equals(canonicalManifest, canonicalBaseline, StringComparison.Ordinal))
            {
                diagnostics.Add(new ManifestDiagnostic(
                    manifestPath,
                    "Manifest differs from recorded baseline snapshot.",
                    ManifestDiagnosticLevel.Error));
            }

            // Secondary guard: ensure every supported capability has a lease declaration.
            if (manifest is not null)
            {
                var missingLease = manifest.SupportedCapabilities.FirstOrDefault(capability =>
                    manifest.CapabilityLeases.All(lease =>
                        !string.Equals(lease.Capability, capability, StringComparison.OrdinalIgnoreCase)));

                if (!string.IsNullOrEmpty(missingLease))
                {
                    diagnostics.Add(new ManifestDiagnostic(
                        manifestPath,
                        $"Capability '{missingLease}' is not covered by a lease declaration.",
                        ManifestDiagnosticLevel.Error));
                }
            }
        }

        // Detect orphaned baselines that no longer have matching manifests.
        var baselineFiles = Directory.EnumerateFiles(baselineRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var baselinePath in baselineFiles)
        {
            var relative = Path.GetRelativePath(baselineRoot, baselinePath);
            var manifestPath = Path.Combine(manifestRoot, relative);
            if (!File.Exists(manifestPath))
            {
                diagnostics.Add(new ManifestDiagnostic(
                    manifestPath,
                    $"Baseline snapshot '{baselinePath}' has no corresponding manifest.",
                    ManifestDiagnosticLevel.Error));
            }
        }

        return new ManifestLintResult(diagnostics);
    }
}

internal sealed record ManifestDiagnostic(
    string ManifestPath,
    string Message,
    ManifestDiagnosticLevel Severity);

internal enum ManifestDiagnosticLevel
{
    Info,
    Warning,
    Error
}

internal sealed class ManifestLintResult
{
    public ManifestLintResult(IReadOnlyList<ManifestDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public IReadOnlyList<ManifestDiagnostic> Diagnostics { get; }

    public bool IsSuccess => Diagnostics.All(d => d.Severity != ManifestDiagnosticLevel.Error);
}
