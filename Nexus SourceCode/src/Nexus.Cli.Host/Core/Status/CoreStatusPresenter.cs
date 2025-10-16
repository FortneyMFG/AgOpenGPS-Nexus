using System.Globalization;
using System.Linq;
using System.Text.Json;
using Nexus.Cli.Host.Core.Endpoints;
using Nexus.Cli.Host.Output;
using Spectre.Console;

namespace Nexus.Cli.Host.Core.Status;

public sealed class CoreStatusPresenter
{
    private static readonly JsonSerializerOptions PrettyJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions CompactJson = new(JsonSerializerDefaults.Web);

    public void Render(CoreStatusSummary summary, OutputFormat format, IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(console);

        switch (format)
        {
            case OutputFormat.Human:
                RenderHuman(summary, console);
                break;
            case OutputFormat.Json:
                console.WriteLine(JsonSerializer.Serialize(CreateReport(summary), PrettyJson));
                break;
            case OutputFormat.Ndjson:
                console.WriteLine(JsonSerializer.Serialize(CreateReport(summary), CompactJson));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.");
        }
    }

    private static void RenderHuman(CoreStatusSummary summary, IAnsiConsole console)
    {
        console.Write(new Rule("[bold green]Nexus Core status[/]") { Alignment = Justify.Left });

        var finalAttempt = summary.FinalAttempt;
        var statusTable = new Table().Border(TableBorder.Rounded).Expand();
        statusTable.AddColumn("[steelblue1]Field[/]");
        statusTable.AddColumn("Value");
        statusTable.AddRow("State", finalAttempt.State.ToString());
        statusTable.AddRow("Transport", FormatTransport(finalAttempt.Endpoint.Transport));
        statusTable.AddRow("Endpoint", finalAttempt.Endpoint.Address);
        statusTable.AddRow("Strategy", summary.Resolution.Strategy);
        statusTable.AddRow("Latency", FormatLatency(finalAttempt.Latency));
        statusTable.AddRow("Message", finalAttempt.Message ?? "(none)");
        console.Write(statusTable);

        var attemptsTable = new Table().Border(TableBorder.Rounded).Expand();
        attemptsTable.AddColumn("#");
        attemptsTable.AddColumn("Transport");
        attemptsTable.AddColumn("Endpoint");
        attemptsTable.AddColumn("State");
        attemptsTable.AddColumn("Latency");
        attemptsTable.AddColumn("Message");

        for (var index = 0; index < summary.Attempts.Count; index++)
        {
            var attempt = summary.Attempts[index];
            attemptsTable.AddRow(
                (index + 1).ToString(CultureInfo.InvariantCulture),
                FormatTransport(attempt.Endpoint.Transport),
                attempt.Endpoint.Address,
                attempt.State.ToString(),
                FormatLatency(attempt.Latency),
                attempt.Message ?? "(none)");
        }

        console.Write(attemptsTable);
    }

    private static string FormatTransport(CoreTransportKind transport) => transport switch
    {
        CoreTransportKind.NamedPipe => "Named pipe",
        CoreTransportKind.UnixDomainSocket => "Unix domain socket",
        CoreTransportKind.Tcp => "TCP",
        _ => transport.ToString(),
    };

    private static string FormatLatency(TimeSpan? latency)
    {
        if (latency is null)
        {
            return "n/a";
        }

        return $"{latency.Value.TotalMilliseconds:F0} ms";
    }

    private static CoreStatusReport CreateReport(CoreStatusSummary summary)
    {
        var candidates = summary.Resolution.Candidates
            .Select(CreateEndpointModel)
            .ToArray();

        var attempts = summary.Attempts
            .Select(attempt => new CoreStatusAttemptModel(
                CreateEndpointModel(attempt.Endpoint),
                attempt.State.ToString(),
                attempt.Latency?.TotalMilliseconds,
                attempt.Message,
                attempt.CheckedAt))
            .ToArray();

        return new CoreStatusReport(
            summary.Resolution.Strategy,
            candidates,
            attempts,
            summary.FinalAttempt.State.ToString(),
            summary.SuccessfulAttempt?.State.ToString());
    }

    private static CoreEndpointModel CreateEndpointModel(CoreEndpoint endpoint)
    {
        return new CoreEndpointModel(
            endpoint.Transport.ToString(),
            endpoint.Address,
            endpoint.Source);
    }

    private sealed record CoreStatusReport(
        string Strategy,
        IReadOnlyList<CoreEndpointModel> Candidates,
        IReadOnlyList<CoreStatusAttemptModel> Attempts,
        string FinalState,
        string? SuccessfulState);

    private sealed record CoreEndpointModel(string Transport, string Address, string Source);

    private sealed record CoreStatusAttemptModel(
        CoreEndpointModel Endpoint,
        string State,
        double? LatencyMilliseconds,
        string? Message,
        DateTimeOffset CheckedAt);
}
