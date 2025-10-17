using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aog.Agio.Linux;
using Aog.Agio.Linux.Serial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class LinuxSerialPortEnumeratorTests : IDisposable
{
    private readonly string _root;

    public LinuxSerialPortEnumeratorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nexus-linux-serial-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void GetPortNames_ReturnsDevicesMatchingPrefixes()
    {
        var devDir = CreateSubDirectory("dev");
        var usb0 = CreateDevice(devDir, "ttyUSB0");
        var usb1 = CreateDevice(devDir, "ttyUSB1");
        var acm0 = CreateDevice(devDir, "ttyACM0");
        _ = CreateDevice(devDir, "random0");

        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[]
            {
                Path.Combine(devDir, "ttyUSB"),
                Path.Combine(devDir, "ttyACM"),
            },
        });

        var enumerator = new LinuxSerialPortEnumerator(NullLogger<LinuxSerialPortEnumerator>.Instance, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Equal(3, ports.Length);
        Assert.Contains(usb0, ports);
        Assert.Contains(usb1, ports);
        Assert.Contains(acm0, ports);
    }

    [Fact]
    public void GetPortNames_EnumeratesDirectoryEntries()
    {
        var byIdDir = CreateSubDirectory("serial/by-id");
        var device = CreateDevice(byIdDir, "u-blox" + Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[]
            {
                Path.Combine(_root, "serial/by-id/")
            },
        });

        var enumerator = new LinuxSerialPortEnumerator(NullLogger<LinuxSerialPortEnumerator>.Instance, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Single(ports);
        Assert.Equal(device, ports[0]);
    }

    [Fact]
    public void GetPortNames_IgnoresDirectoryEntries()
    {
        var devDir = CreateSubDirectory("dev");
        var device = CreateDevice(devDir, "ttyUSB0");
        var directoryEntry = CreateSubDirectory(Path.Combine("dev", "ttyUSB1"));

        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[]
            {
                devDir.TrimEnd('/', '\\') + "/",
            },
        });

        var logger = new ListLogger<LinuxSerialPortEnumerator>();
        var enumerator = new LinuxSerialPortEnumerator(logger, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Single(ports);
        Assert.Equal(device, ports[0]);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains(directoryEntry));
    }

    [Fact]
    public void GetPortNames_NoPrefixesConfigured_ReturnsEmptyAndLogs()
    {
        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = Array.Empty<string>(),
        });

        var logger = new ListLogger<LinuxSerialPortEnumerator>();
        var enumerator = new LinuxSerialPortEnumerator(logger, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Empty(ports);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("no device prefixes"));
    }

    [Fact]
    public void ConfigureServices_CustomPrefixesFromConfiguration_ReplaceDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AgioHost:Linux:Serial:DevicePrefixes:0"] = "/dev/custom0",
                ["AgioHost:Linux:Serial:DevicePrefixes:1"] = "/dev/custom1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        var backend = new LinuxAgioBackend();
        backend.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LinuxSerialPortEnumeratorOptions>>().Value;

        Assert.Equal(new[] { "/dev/custom0", "/dev/custom1" }, options.DevicePrefixes);
    }

    [Fact]
    public void DevicePrefixes_SetterClonesAssignedArray()
    {
        var prefixes = new[] { "/dev/original" };
        var options = new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = prefixes,
        };

        prefixes[0] = "/dev/mutated";

        Assert.Equal("/dev/original", options.DevicePrefixes[0]);
    }

    [Fact]
    public void GetPortNames_SymlinkAndCanonicalPath_CollapsesToSingleEntry()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var devDir = CreateSubDirectory("dev");
        var byIdDir = CreateSubDirectory(Path.Combine("dev", "serial", "by-id"));
        var device = CreateDevice(devDir, "ttyACM0");

        var symlinkPath = Path.Combine(byIdDir, "gnss-device");
        if (File.Exists(symlinkPath))
        {
            File.Delete(symlinkPath);
        }

        File.CreateSymbolicLink(symlinkPath, device);

        var logger = new ListLogger<LinuxSerialPortEnumerator>();
        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[]
            {
                Path.Combine(devDir, "ttyACM"),
                byIdDir + "/",
            },
        });

        var enumerator = new LinuxSerialPortEnumerator(logger, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Single(ports);
        var symlinkFullPath = Path.GetFullPath(symlinkPath);
        Assert.Contains(ports[0], new[] { device, symlinkFullPath });
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Debug && entry.Message.Contains(symlinkFullPath, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in CI.
        }
    }

    private string CreateSubDirectory(string relative)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateDevice(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, string.Empty);
        return Path.GetFullPath(path);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        private sealed class Scope : IDisposable
        {
            public static Scope Instance { get; } = new();

            public void Dispose()
            {
            }
        }

        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Scope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
