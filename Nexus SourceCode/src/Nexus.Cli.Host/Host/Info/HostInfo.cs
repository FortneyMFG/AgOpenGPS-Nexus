namespace Nexus.Cli.Host.Host;

public sealed record HostInfo(
    string Version,
    string Framework,
    string RuntimeIdentifier,
    string OperatingSystem,
    string ProcessArchitecture,
    ConsoleProfile Console,
    EnvironmentPaths Environment);

public sealed record ConsoleProfile(
    bool SupportsAnsi,
    bool SupportsLinks,
    bool SupportsInteractive,
    string ColorSystem);

public sealed record EnvironmentPaths(
    string UserDirectory,
    string ConfigFilePath,
    string CredentialsFilePath,
    string CacheDirectory,
    string PluginsDirectory,
    string RepositoryConfigDirectory);
