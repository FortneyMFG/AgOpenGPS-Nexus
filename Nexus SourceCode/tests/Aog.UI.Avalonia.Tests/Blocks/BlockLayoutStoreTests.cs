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
        instances.Should().Contain(i => i.DefinitionId.Value == "Menu.EquipmentControls" && i.Origin == BlockOrigin.Canonical);
        instances.Should().Contain(i => i.DefinitionId.Value == "Cmd.AutoSteerToggle" && i.Origin == BlockOrigin.Canonical);
        instances.Should().Contain(i => i.DefinitionId.Value == "Cmd.AutoSteerToggle" && i.Origin == BlockOrigin.Clone && i.Region == BlockRegion.Right);
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

        public void UpdateRunMode(Hosting.AvaloniaRunMode mode) => throw new System.NotImplementedException();

        public void UpdateShellLayout(ShellLayoutPreferences layout)
        {
            _preferences.ShellLayout = layout.Clone();
        }
    }
}
