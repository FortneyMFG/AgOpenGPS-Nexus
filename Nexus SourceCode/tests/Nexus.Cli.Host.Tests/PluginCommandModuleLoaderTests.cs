using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Aog.Plugins;
using Nexus.Cli.Host.Plugins;
using Nexus.Cli.Host.Runtime;
using Nexus.Plugin.Cli.Abstractions;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public sealed class PluginCommandModuleLoaderTests : IDisposable
{
    private readonly string _root;
    private readonly TestConsole _console = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeEnvironment _environment;
    private readonly PluginManifestLoader _manifestLoader = new();

    public PluginCommandModuleLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"nx-cli-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "plugins"));

        var services = new ServiceCollection();
        _environment = new FakeEnvironment(_root);
        services.AddSingleton<INexusEnvironment>(_environment);
        services.AddSingleton<IAnsiConsole>(_console);
        services.AddSingleton(TimeProvider.System);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task LoadModulesAsync_LoadsModulesFromCliAdapters()
    {
        var pluginDir = PreparePluginDirectory("org.agopengps.sample", "1.2.3");
        var assemblyPath = Path.Combine(pluginDir, "SamplePlugin.Cli.dll");
        await CreateCliAssemblyAsync(assemblyPath);

        var loader = new PluginCommandModuleLoader(_environment, _serviceProvider, _manifestLoader, _console);

        var modules = await loader.LoadModulesAsync(CancellationToken.None);

        modules.Should().HaveCount(1);

        var rootCommand = new RootCommand();
        var context = new CommandModuleContext(rootCommand, _serviceProvider);
        modules[0].Configure(context);

        rootCommand.Children.Should().ContainSingle();
        var command = rootCommand.Children.Single();
        command.Name.Should().Be("sample");
        command.Description.Should().Be("Sample Plugin");
    }

    [Fact]
    public async Task LoadModulesAsync_SkipsInvalidManifest()
    {
        var pluginDir = Path.Combine(_environment.PluginsDirectory, "org.agopengps.invalid", "1.0.0");
        Directory.CreateDirectory(pluginDir);
        await File.WriteAllTextAsync(Path.Combine(pluginDir, "plugin.json"), "{\n  \"schemaVersion\": \"1.0.0\"\n}");

        var loader = new PluginCommandModuleLoader(_environment, _serviceProvider, _manifestLoader, _console);

        var modules = await loader.LoadModulesAsync(CancellationToken.None);

        modules.Should().BeEmpty();
        _console.Output.Should().Contain("Failed to read plugin manifest");
    }

    private string PreparePluginDirectory(string pluginId, string version)
    {
        var pluginRoot = Path.Combine(_environment.PluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginRoot);
        var versionDirectory = Path.Combine(pluginRoot, version);
        Directory.CreateDirectory(versionDirectory);

        var manifest = CreateManifest(pluginId, version);
        File.WriteAllText(Path.Combine(versionDirectory, "plugin.json"), manifest);

        return versionDirectory;
    }

    private static string CreateManifest(string pluginId, string version)
    {
        return $$"""
        {
          "schemaVersion": "1.0.0",
          "id": "{{pluginId}}",
          "name": "Sample Plugin",
          "version": "{{version}}",
          "description": "Sample CLI plugin",
          "author": "Nexus",
          "license": "MIT",
          "targets": {
            "core": ">=1.0.0",
            "ui": ">=1.0.0"
          },
          "entry": {
            "backend": {
              "exe": "run.sh"
            }
          },
          "permissions": {},
          "requiredApis": {
            "core": ">=1.0.0"
          },
          "supportedCapabilities": ["guidance.sample"],
          "requiredTransports": ["AOG-Link"],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": [
            {
              "providerId": "sample.provider",
              "type": "Sample.Provider",
              "topics": ["topic"]
            }
          ],
          "leases": [
            {
              "capability": "guidance.sample",
              "mode": "Exclusive",
              "timeoutSeconds": 5,
              "recovery": "GracefulDegradation"
            }
          ]
        }
        """;
    }

    private static async Task CreateCliAssemblyAsync(string assemblyPath)
    {
        var code = """
        using System;
        using System.CommandLine;
        using Nexus.Plugin.Cli.Abstractions;

        public sealed class SampleModule : ICommandModule
        {
            public void Configure(CommandModuleContext context)
            {
                var descriptor = (PluginCommandModuleDescriptor?)context.Services.GetService(typeof(PluginCommandModuleDescriptor));
                var name = descriptor?.PluginName ?? "Sample";
                var command = new Command("sample", name);
                command.SetHandler(() => Console.WriteLine("sample"));
                context.RootCommand.AddCommand(command);
            }
        }
        """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Command).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CommandModuleContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PluginCommandModuleDescriptor).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "SamplePlugin.Cli",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        await using var stream = File.Open(assemblyPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Failed to compile plugin CLI assembly:");
            foreach (var diagnostic in emitResult.Diagnostics)
            {
                builder.AppendLine(diagnostic.ToString());
            }

            throw new InvalidOperationException(builder.ToString());
        }
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
            // Ignore cleanup errors in test runs.
        }

        _serviceProvider.Dispose();
    }

    private sealed class FakeEnvironment : INexusEnvironment
    {
        public FakeEnvironment(string userDirectory)
        {
            UserDirectoryValue = userDirectory;
        }

        public string UserDirectoryValue { get; }

        public string UserDirectory => UserDirectoryValue;

        public string ConfigFilePath => Path.Combine(UserDirectoryValue, "config.yml");

        public string CredentialsFilePath => Path.Combine(UserDirectoryValue, "credentials");

        public string CacheDirectory => Path.Combine(UserDirectoryValue, "cache");

        public string PluginsDirectory => Path.Combine(UserDirectoryValue, "plugins");

        public string RepositoryConfigDirectory => Path.Combine(UserDirectoryValue, "repo");
    }
}
