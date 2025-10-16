using System.CommandLine;

namespace Nexus.Cli.Host.Output;

public static class OutputOptions
{
    public static Option<OutputFormat> ModeOption { get; } = CreateOption();

    private static Option<OutputFormat> CreateOption()
    {
        var option = new Option<OutputFormat>(
            aliases: new[] { "--output", "--format" },
            description: "Controls CLI output formatting (human, json, ndjson).",
            parseArgument: result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return OutputFormat.Human;
                }

                var token = result.Tokens.Single().Value;
                if (Enum.TryParse<OutputFormat>(token, ignoreCase: true, out var format))
                {
                    return format;
                }

                result.ErrorMessage = "Invalid output format. Use 'human', 'json', or 'ndjson'.";
                return OutputFormat.Human;
            })
        {
            Arity = ArgumentArity.ZeroOrOne,
        };

        option.AddAlias("-o");
        option.AddCompletions("human", "json", "ndjson");

        return option;
    }
}
