using System.Diagnostics;

var repositoryRoot = LocateRepositoryRoot();
var projectPath = Path.Combine(repositoryRoot, "Nexus SourceCode", "tools", "generate-baseline", "generate-baseline.csproj");
var arguments = string.Join(" ", args.Select(QuoteArgument));
var processArgs = string.IsNullOrWhiteSpace(arguments)
    ? $"run --project \"{projectPath}\""
    : $"run --project \"{projectPath}\" -- {arguments}";

var processStartInfo = new ProcessStartInfo("dotnet", processArgs)
{
    WorkingDirectory = repositoryRoot,
    UseShellExecute = false,
};

using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start baseline generator.");
process.WaitForExit();

if (process.ExitCode != 0)
{
    throw new InvalidOperationException($"Baseline generator failed with exit code {process.ExitCode}.");
}

static string LocateRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Unable to locate repository root.");
}

static string QuoteArgument(string argument)
{
    if (string.IsNullOrEmpty(argument))
    {
        return "\"\"";
    }

    return argument.Contains(' ') ? $"\"{argument}\"" : argument;
}
