using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

var options = BaselineGenerationOptions.Parse(args);
var repositoryRoot = LocateRepositoryRoot();
var nexusRoot = Path.Combine(repositoryRoot, "Nexus SourceCode");
var abstractionsProjectPath = Path.Combine(nexusRoot, "src", "Aog.Abstractions", "Aog.Abstractions.csproj");

if (!File.Exists(abstractionsProjectPath))
{
    throw new InvalidOperationException($"Unable to find Aog.Abstractions project at '{abstractionsProjectPath}'.");
}

if (!options.SkipBuild)
{
    BuildProject(repositoryRoot, abstractionsProjectPath, options.Configuration);
}

var assemblyPath = Path.Combine(nexusRoot, "src", "Aog.Abstractions", "bin", options.Configuration, options.TargetFramework, "Aog.Abstractions.dll");
if (!File.Exists(assemblyPath))
{
    throw new FileNotFoundException($"Expected assembly not found at '{assemblyPath}'.", assemblyPath);
}

var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
var registryType = assembly.GetType("Aog.Abstractions.Contracts.GrpcContractRegistry")
    ?? throw new InvalidOperationException("GrpcContractRegistry type not found in Aog.Abstractions assembly.");
var descriptorProperty = registryType.GetProperty("DescriptorSet", BindingFlags.Public | BindingFlags.Static)
    ?? throw new InvalidOperationException("DescriptorSet property not found on GrpcContractRegistry.");
var descriptor = descriptorProperty.GetValue(null) ?? throw new InvalidOperationException("DescriptorSet returned null.");
var toByteArray = descriptor.GetType().GetMethod("ToByteArray", Type.EmptyTypes)
    ?? throw new InvalidOperationException("DescriptorSet did not expose a ToByteArray method.");
var bytes = (byte[])toByteArray.Invoke(descriptor, Array.Empty<object>())!;
var base64 = Convert.ToBase64String(bytes);

var baselineDirectory = Path.Combine(nexusRoot, "tests", "Aog.Abstractions.Tests", "Baselines");
Directory.CreateDirectory(baselineDirectory);
var baselinePath = Path.Combine(baselineDirectory, "aog-contracts.desc.base64");
File.WriteAllText(baselinePath, base64);

Console.WriteLine($"Wrote descriptor baseline to {baselinePath}.");

static void BuildProject(string workingDirectory, string projectPath, string configuration)
{
    var args = $"build \"{projectPath}\" --configuration {configuration}";
    var process = Process.Start(new ProcessStartInfo("dotnet", args)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
    }) ?? throw new InvalidOperationException("Failed to start dotnet build.");

    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"dotnet build exited with code {process.ExitCode}.");
    }
}

static string LocateRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Unable to locate repository root.");
}

internal readonly record struct BaselineGenerationOptions(string Configuration, string TargetFramework, bool SkipBuild)
{
    private const string DefaultConfiguration = "Debug";
    private const string DefaultTargetFramework = "net8.0";

    public static BaselineGenerationOptions Parse(string[] arguments)
    {
        var configuration = DefaultConfiguration;
        var targetFramework = DefaultTargetFramework;
        var skipBuild = false;

        foreach (var argument in arguments)
        {
            if (argument.StartsWith("--configuration=", StringComparison.OrdinalIgnoreCase))
            {
                configuration = argument.Split('=')[1];
            }
            else if (argument.StartsWith("--framework=", StringComparison.OrdinalIgnoreCase) || argument.StartsWith("--tfm=", StringComparison.OrdinalIgnoreCase))
            {
                targetFramework = argument.Split('=')[1];
            }
            else if (argument.Equals("--skip-build", StringComparison.OrdinalIgnoreCase))
            {
                skipBuild = true;
            }
        }

        return new BaselineGenerationOptions(configuration, targetFramework, skipBuild);
    }
}
