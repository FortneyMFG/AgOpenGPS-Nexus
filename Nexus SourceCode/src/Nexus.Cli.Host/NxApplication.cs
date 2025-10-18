using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace Nexus.Cli.Host;

public sealed class NxApplication
{
    private readonly RootCommand _rootCommand;
    private Parser? _parser;

    public NxApplication(RootCommand rootCommand)
    {
        _rootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
    }

    public Task<int> InvokeAsync(string[] args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        cancellationToken.ThrowIfCancellationRequested();

        var parser = _parser ??= BuildParser();
        return parser.InvokeAsync(args, console: null);
    }

    private Parser BuildParser()
    {
        return new CommandLineBuilder(_rootCommand)
            .UseDefaults()
            .Build();
    }
}
