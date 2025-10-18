using System.Text.Json;
using Nexus.Cli.Host.Output;
using Spectre.Console;

namespace Nexus.Cli.Host.Host;

public sealed class HostInfoPresenter
{
    private static readonly JsonSerializerOptions PrettyJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions CompactJson = new(JsonSerializerDefaults.Web);

    public void Render(HostInfo hostInfo, OutputFormat format, IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(hostInfo);
        ArgumentNullException.ThrowIfNull(console);

        switch (format)
        {
            case OutputFormat.Human:
                RenderHuman(hostInfo, console);
                break;
            case OutputFormat.Json:
                console.WriteLine(JsonSerializer.Serialize(hostInfo, PrettyJson));
                break;
            case OutputFormat.Ndjson:
                console.WriteLine(JsonSerializer.Serialize(hostInfo, CompactJson));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.");
        }
    }

    private static void RenderHuman(HostInfo hostInfo, IAnsiConsole console)
    {
        console.Write(new Rule("[bold green]Nexus CLI host diagnostics[/]") { Justification = Justify.Left });

        var runtimeTable = new Table().Border(TableBorder.Rounded).Expand();
        runtimeTable.AddColumn("[steelblue1]Runtime[/]");
        runtimeTable.AddColumn("Value");
        runtimeTable.AddRow("Version", hostInfo.Version);
        runtimeTable.AddRow("Framework", hostInfo.Framework);
        runtimeTable.AddRow("Runtime ID", hostInfo.RuntimeIdentifier);
        runtimeTable.AddRow("Operating System", hostInfo.OperatingSystem);
        runtimeTable.AddRow("Process Architecture", hostInfo.ProcessArchitecture);
        console.Write(runtimeTable);

        var consoleTable = new Table().Border(TableBorder.Rounded).Expand();
        consoleTable.AddColumn("[steelblue1]Console[/]");
        consoleTable.AddColumn("Value");
        consoleTable.AddRow("ANSI", FormatBoolean(hostInfo.Console.SupportsAnsi));
        consoleTable.AddRow("Links", FormatBoolean(hostInfo.Console.SupportsLinks));
        consoleTable.AddRow("Interactive", FormatBoolean(hostInfo.Console.SupportsInteractive));
        consoleTable.AddRow("Color System", hostInfo.Console.ColorSystem);
        console.Write(consoleTable);

        var environmentTable = new Table().Border(TableBorder.Rounded).Expand();
        environmentTable.AddColumn("[steelblue1]Environment[/]");
        environmentTable.AddColumn("Path");
        environmentTable.AddRow("User", hostInfo.Environment.UserDirectory);
        environmentTable.AddRow("Config", hostInfo.Environment.ConfigFilePath);
        environmentTable.AddRow("Credentials", hostInfo.Environment.CredentialsFilePath);
        environmentTable.AddRow("Cache", hostInfo.Environment.CacheDirectory);
        environmentTable.AddRow("Plugins", hostInfo.Environment.PluginsDirectory);
        environmentTable.AddRow("Repository", hostInfo.Environment.RepositoryConfigDirectory);
        console.Write(environmentTable);
    }

    private static string FormatBoolean(bool value) => value ? "yes" : "no";
}
