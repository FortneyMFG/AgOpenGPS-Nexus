using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class AppXamlTests
{
    private static readonly string SrcDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../src/Aog.UI.Avalonia"));

    [Fact]
    public void AppAxaml_ShouldContainSingleApplicationDefinition()
    {
        var appXamlPath = Path.Combine(SrcDirectory, "App.axaml");
        Assert.True(File.Exists(appXamlPath), $"Could not find App.axaml at '{appXamlPath}'.");

        var document = XDocument.Load(appXamlPath);
        var root = document.Root;
        Assert.NotNull(root);
        Assert.Equal("Application", root!.Name.LocalName);

        XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        var xClass = root.Attribute(xamlNamespace + "Class");
        Assert.NotNull(xClass);
        Assert.Equal("Aog.UI.Avalonia.App", xClass!.Value);

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
                Assert.True(false, $"Duplicate x:Class '{className}' discovered in '{existingPath}' and '{path}'.");
            }
            else
            {
                seen[className] = path;
            }
        }
    }
}
