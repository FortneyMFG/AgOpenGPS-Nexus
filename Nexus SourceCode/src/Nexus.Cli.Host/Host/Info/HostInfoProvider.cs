using System.Reflection;
using System.Runtime.InteropServices;
using Nexus.Cli.Host.Runtime;
using Spectre.Console;

namespace Nexus.Cli.Host.Host;

public sealed class HostInfoProvider : IHostInfoProvider
{
    private readonly INexusEnvironment _environment;
    private readonly IAnsiConsole _console;

    public HostInfoProvider(INexusEnvironment environment, IAnsiConsole console)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public HostInfo Create()
    {
        var assembly = typeof(Program).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        var profile = _console.Profile.Capabilities;

        var environmentPaths = new EnvironmentPaths(
            _environment.UserDirectory,
            _environment.ConfigFilePath,
            _environment.CredentialsFilePath,
            _environment.CacheDirectory,
            _environment.PluginsDirectory,
            _environment.RepositoryConfigDirectory);

        var consoleProfile = new ConsoleProfile(
            profile.Ansi,
            profile.Links,
            profile.Interactive,
            profile.ColorSystem.ToString());

        return new HostInfo(
            version,
            RuntimeInformation.FrameworkDescription.Trim(),
            RuntimeInformation.RuntimeIdentifier,
            RuntimeInformation.OSDescription.Trim(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            consoleProfile,
            environmentPaths);
    }
}
