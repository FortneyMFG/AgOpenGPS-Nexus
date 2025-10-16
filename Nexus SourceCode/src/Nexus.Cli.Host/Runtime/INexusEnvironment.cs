namespace Nexus.Cli.Host.Runtime;

public interface INexusEnvironment
{
    string UserDirectory { get; }

    string ConfigFilePath { get; }

    string CredentialsFilePath { get; }

    string CacheDirectory { get; }

    string PluginsDirectory { get; }

    string RepositoryConfigDirectory { get; }
}
