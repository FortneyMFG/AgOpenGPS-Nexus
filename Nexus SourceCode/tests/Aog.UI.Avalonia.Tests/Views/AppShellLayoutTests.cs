using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Views;

public sealed class AppShellLayoutTests
{
    private static readonly XNamespace AvaloniaNs = "https://github.com/avaloniaui";
    private static readonly string SrcDirectory = LocateSrcDirectory();

    [Fact]
    public void AppShellUsesSingleItemsPanel()
    {
        var doc = LoadShellView();
        var itemsPanels = doc.Descendants(AvaloniaNs + "ItemsControl")
            .SelectMany(control => control.Elements(AvaloniaNs + "ItemsControl.ItemsPanel"));
        itemsPanels.Should().ContainSingle();
        var tiledPanel = doc.Descendants().FirstOrDefault(element => element.Name.LocalName == "TiledPanel");
        tiledPanel.Should().NotBeNull();
    }

    [Fact]
    public void AppShellDoesNotDeclareLegacySidebarControls()
    {
        var text = File.ReadAllText(ShellViewPath);
        text.Should().NotContain("LeftBlocksControl");
        text.Should().NotContain("RightBlocksControl");
        text.Should().NotContain("TopBlocksControl");
        text.Should().NotContain("BottomBlocksControl");
        text.Should().NotContain("WrapPanel Orientation=\"Vertical\"");
        text.Should().NotContain("WrapPanel Orientation=\"Horizontal\"");
    }

    private static XDocument LoadShellView() => XDocument.Load(ShellViewPath);

    private static string ShellViewPath => Path.Combine(
        SrcDirectory,
        "Views",
        "Shell",
        "AppShellView.axaml");

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
