using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Controls the lifecycle of backend helper processes (core host, AgIO, simulator).
/// </summary>
public sealed class BackendServiceManager
{
    private readonly ILogger<BackendServiceManager> _logger;
    private readonly Dictionary<BackendServiceKind, Process> _running = new();
    private readonly Dictionary<BackendServiceKind, BackendServiceDescriptor> _descriptors;
    private readonly object _sync = new();

    public BackendServiceManager(ILogger<BackendServiceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var repoRoot = LocateRepositoryRoot();
        _descriptors = new Dictionary<BackendServiceKind, BackendServiceDescriptor>
        {
            [BackendServiceKind.CoreHost] = new(
                "Core Host",
                Path.Combine(repoRoot, "src", "Aog.Core.Host", "Aog.Core.Host.csproj")),
            [BackendServiceKind.AgioWindows] = new(
                "AgIO (Windows)",
                Path.Combine(repoRoot, "src", "Aog.Agio.Windows", "Aog.Agio.Windows.csproj")),
            [BackendServiceKind.AgioSim] = new(
                "AgIO Simulator",
                Path.Combine(repoRoot, "src", "Aog.Agio.Sim", "Aog.Agio.Sim.csproj")),
        };
    }

    public Task<bool> StartAsync(BackendServiceKind kind, CancellationToken cancellationToken)
    {
        if (!_descriptors.TryGetValue(kind, out var descriptor))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), $"Unknown backend service '{kind}'.");
        }

        lock (_sync)
        {
            if (_running.ContainsKey(kind))
            {
                _logger.LogInformation("Service {ServiceName} is already running.", descriptor.DisplayName);
                return Task.FromResult(false);
            }
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(descriptor.ProjectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(descriptor.ProjectPath);

        try
        {
            var process = Process.Start(psi);
            if (process is null)
            {
                throw new InvalidOperationException($"Failed to start {descriptor.DisplayName}.");
            }

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                lock (_sync)
                {
                    _running.Remove(kind);
                }
            };

            lock (_sync)
            {
                _running[kind] = process;
            }

            _logger.LogInformation("Launched service {ServiceName} (PID {Pid}).", descriptor.DisplayName, process.Id);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service {ServiceName}.", descriptor.DisplayName);
            throw;
        }
    }

    public Task<bool> StopAsync(BackendServiceKind kind, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_running.TryGetValue(kind, out var process))
            {
                return Task.FromResult(false);
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit();
                }

                _logger.LogInformation("Stopped service {ServiceName}.", _descriptors[kind].DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while stopping service {ServiceName}.", _descriptors[kind].DisplayName);
            }
            finally
            {
                _running.Remove(kind);
            }
        }

        return Task.FromResult(true);
    }

    private static string LocateRepositoryRoot()
    {
        // AppContext.BaseDirectory -> .../src/Aog.UI.Avalonia/bin/<Configuration>/net8.0/
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Unable to resolve repository root from {AppContext.BaseDirectory}.");
        }

        return root;
    }

    private sealed record BackendServiceDescriptor(string DisplayName, string ProjectPath);
}

public enum BackendServiceKind
{
    CoreHost,
    AgioWindows,
    AgioSim,
}
