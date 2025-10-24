using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class AppXamlTests
{
    private static readonly string SrcDirectory = LocateSrcDirectory();

    [Fact]
    public void AppAxaml_ShouldContainSingleApplicationDefinition()
    {
        var appXamlPath = Path.Combine(SrcDirectory, "App", "NexusApp.axaml");
        Assert.True(File.Exists(appXamlPath), $"Could not find NexusApp.axaml at '{appXamlPath}'.");

        var document = XDocument.Load(appXamlPath);
        var root = document.Root;
        Assert.NotNull(root);
        Assert.Equal("Application", root!.Name.LocalName);

        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        var xClass = root.Attribute(xamlNamespace + "Class");
        Assert.NotNull(xClass);
        Assert.Equal("Aog.UI.Avalonia.App.NexusApp", xClass!.Value);

        var nestedApplications = root
            .Descendants()
            .Where(element => element.Name.LocalName == "Application");

        Assert.Empty(nestedApplications);
    }

    [Fact]
    public void AxamlResources_ShouldHaveUniqueXClassValues()
    {
        Assert.True(Directory.Exists(SrcDirectory), $"Could not locate src folder at '{SrcDirectory}'.");

        var xamlFiles = Directory.GetFiles(SrcDirectory, "*.axaml", SearchOption.AllDirectories);
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var path in xamlFiles)
        {
            var document = XDocument.Load(path);
            var root = document.Root;
            if (root == null)
            {
                continue;
            }

            XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
            var xClass = root.Attribute(xamlNamespace + "Class");
            if (xClass == null)
            {
                continue;
            }

            var className = xClass.Value;
            if (seen.TryGetValue(className, out var existingPath))
            {
                Assert.Fail($"Duplicate x:Class '{className}' discovered in '{existingPath}' and '{path}'.");
            }

            seen[className] = path;
        }
    }

    private static string LocateSrcDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.Exists)
        {
            var candidate = Path.Combine(directory.FullName, "Nexus SourceCode", "src", "Aog.UI.Avalonia");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Unable to locate repository root containing 'Nexus SourceCode/src/Aog.UI.Avalonia' starting from '{AppContext.BaseDirectory}'.");
    }
}
