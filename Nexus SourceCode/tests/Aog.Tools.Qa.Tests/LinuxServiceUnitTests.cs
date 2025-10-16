using System.IO;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.Qa.Tests;

public sealed class LinuxServiceUnitTests
{
    [Fact]
    public void CoreService_Unit_Uses_Hardened_Defaults()
    {
        var unitPath = Path.Combine(GetRepoRoot(), "tools", "packaging", "linux", "systemd", "aog-core.service");
        File.Exists(unitPath).Should().BeTrue();
        var contents = File.ReadAllText(unitPath);

        contents.Should().Contain("ExecStart=/usr/bin/aog-core");
        contents.Should().Contain("EnvironmentFile=-/etc/aog/core.env");
        contents.Should().Contain("User=aogsvc");
        contents.Should().Contain("StateDirectory=aog");
        contents.Should().Contain("LogsDirectory=aog");
        contents.Should().Contain("ProtectSystem=full");
        contents.Should().Contain("AmbientCapabilities=CAP_NET_BIND_SERVICE");
    }

    [Fact]
    public void AgioService_Unit_Configures_Device_Access()
    {
        var unitPath = Path.Combine(GetRepoRoot(), "tools", "packaging", "linux", "systemd", "aog-agio.service");
        File.Exists(unitPath).Should().BeTrue();
        var contents = File.ReadAllText(unitPath);

        contents.Should().Contain("ExecStart=/usr/bin/aog-agio");
        contents.Should().Contain("EnvironmentFile=-/etc/aog/agio.env");
        contents.Should().Contain("SupplementaryGroups=dialout plugdev");
        contents.Should().Contain("Restart=on-failure");
        contents.Should().Contain("StateDirectory=aog");
        contents.Should().Contain("LogsDirectory=aog");
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "SERVICES.md");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to resolve repository root from test context.");
    }
}
