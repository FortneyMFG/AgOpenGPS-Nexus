using System.IO;
using System.Threading;

namespace Nexus.Cli.Host.Runtime;

public sealed class NexusEnvironment : INexusEnvironment
{
    private readonly Lazy<string> _userProfile = new(ResolveUserProfile, LazyThreadSafetyMode.ExecutionAndPublication);

    public string UserDirectory => Path.Combine(UserProfile, ".nexus");

    public string ConfigFilePath => Path.Combine(UserDirectory, "config.yml");

    public string CredentialsFilePath => Path.Combine(UserDirectory, "credentials");

    public string CacheDirectory => Path.Combine(UserDirectory, "cache");

    public string PluginsDirectory => Path.Combine(UserDirectory, "plugins");

    public string RepositoryConfigDirectory => Path.Combine(Environment.CurrentDirectory, ".nexus");

    private string UserProfile => _userProfile.Value;

    private static string ResolveUserProfile()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
        {
            return profile;
        }

        profile = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(profile))
        {
            return profile;
        }

        profile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrWhiteSpace(profile))
        {
            return profile;
        }

        return Environment.CurrentDirectory;
    }
}
