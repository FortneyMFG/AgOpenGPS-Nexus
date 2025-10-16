using System.CommandLine.Invocation;

namespace Nexus.Cli.Host.Output;

public static class InvocationContextExtensions
{
    public static OutputFormat GetOutputFormat(this InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.ParseResult.GetValueForOption(OutputOptions.ModeOption);
    }

    public static CancellationToken GetCancellationTokenSafe(this InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return System.CommandLine.InvocationExtensions.GetCancellationToken(context);
    }
}
