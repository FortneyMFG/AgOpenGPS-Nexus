using System.Linq;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Settings;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Blocks;

public class BlockLayoutStoreTests
{
    [Fact]
    public void Load_SeedsCanonicalAndCloneLayoutWhenEmpty()
    {
        var preferences = new UiPreferences();
        var service = new FakePreferencesService(preferences);
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var store = new BlockLayoutStore(service, catalog);

        var instances = store.Load();

        instances.Should().NotBeEmpty();
        instances.Should().Contain(i => i.DefinitionId.Value == "Menu.FieldSettings" && i.Origin == BlockOrigin.Canonical);
        instances.Should().Contain(i => i.DefinitionId.Value == "Cmd.FieldBoundaries" && i.Origin == BlockOrigin.Canonical);
        instances.Should().Contain(i => i.DefinitionId.Value == "Cmd.FieldSettings" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Left);
        instances.Should().Contain(i => i.DefinitionId.Value == "Info.Speed" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Left && i.GroupKey == "12.5 km/h");
        instances.Should().Contain(i => i.DefinitionId.Value == "Info.Heading" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Left && i.GroupKey == "N 45°");
        instances.Should().Contain(i => i.DefinitionId.Value == "Info.Altitude" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Left && i.GroupKey == "342 m");
        instances.Should().Contain(i => i.DefinitionId.Value == "Info.Steering" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Right && i.GroupKey == "Auto");
        instances.Should().Contain(i => i.DefinitionId.Value == "Info.Section2" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Right && i.GroupKey == "OFF");
        preferences.ShellLayout.Instances.Should().NotBeEmpty();
    }

    [Fact]
    public void Save_ReplacesInstancesAndPersists()
    {
        var preferences = new UiPreferences();
        var service = new FakePreferencesService(preferences);
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var store = new BlockLayoutStore(service, catalog);
        var custom = new[]
        {
            new BlockInstance
            {
                DefinitionId = new BlockDefinitionId("Cmd.Start"),
                Origin = BlockOrigin.Clone,
                Region = BlockRegion.Bottom,
                Order = 5,
            },
        };

        store.Save(custom);

        preferences.ShellLayout.Instances.Should().HaveCount(1);
        preferences.ShellLayout.Instances.Single().DefinitionId.Value.Should().Be("Cmd.Start");
    }

    [Fact]
    public void Load_RemovesInstancesWithUnknownDefinitions()
    {
        var preferences = new UiPreferences();
        preferences.ShellLayout.Instances.Add(new BlockInstance
        {
            DefinitionId = new BlockDefinitionId("Cmd.Start"),
            Origin = BlockOrigin.Clone,
            Region = BlockRegion.Bottom,
            Order = 0,
        });
        preferences.ShellLayout.Instances.Add(new BlockInstance
        {
            DefinitionId = new BlockDefinitionId("Cmd.Legacy"),
            Origin = BlockOrigin.Clone,
            Region = BlockRegion.Bottom,
            Order = 1,
        });

        var service = new FakePreferencesService(preferences);
        var catalog = new BlockCatalog(new IBlockProvider[] { new CoreBlockProvider() });
        var store = new BlockLayoutStore(service, catalog);

        var instances = store.Load();

        instances.Should().NotContain(instance => instance.DefinitionId.Value == "Cmd.Legacy");
        preferences.ShellLayout.Instances.Should().NotContain(instance => instance.DefinitionId.Value == "Cmd.Legacy");
        preferences.ShellLayout.Instances.Should().Contain(instance => instance.DefinitionId.Value == "Cmd.Start");
    }

    private sealed class FakePreferencesService : IUiPreferencesService
    {
        private readonly UiPreferences _preferences;

        public FakePreferencesService(UiPreferences preferences)
        {
            _preferences = preferences;
        }

        public UiPreferences GetPreferences() => _preferences.Clone();

        public void UpdateTheme(UiTheme theme) => throw new System.NotImplementedException();

        public void UpdateWindowPlacement(WindowPlacement placement) => throw new System.NotImplementedException();

        public void UpdateTelemetryOptIn(bool isOptedIn) => throw new System.NotImplementedException();

        public void UpdateRunMode(Aog.UI.Avalonia.Hosting.AvaloniaRunMode mode) => throw new System.NotImplementedException();

        public void UpdateShellLayout(ShellLayoutPreferences layout)
        {
            _preferences.ShellLayout = layout.Clone();
        }
    }
}
