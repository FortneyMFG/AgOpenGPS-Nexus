using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels.Shell;
using Avalonia;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Blocks;

public sealed class BlockLayoutViewModelTests
{
    [Fact]
    public void UpdateViewport_SnapsTilesToGrid()
    {
        var layoutStore = new FakeLayoutStore(new[]
        {
            new BlockInstance
            {
                InstanceId = new BlockInstanceId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
                DefinitionId = new BlockDefinitionId("Cmd.Start"),
                Region = BlockRegion.Floating,
            },
            new BlockInstance
            {
                InstanceId = new BlockInstanceId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
                DefinitionId = new BlockDefinitionId("Tel.Speed"),
                Region = BlockRegion.Floating,
                SizeOverride = BlockSize.Tile1xHalf,
            },
        });

        var catalog = new FakeCatalog(new[]
        {
            new BlockDefinition
            {
                Id = new BlockDefinitionId("Cmd.Start"),
                PreferredSize = BlockSize.Tile1x1,
                Kind = BlockKind.CommandButton,
                Placement = PlacementPolicy.Free,
            },
            new BlockDefinition
            {
                Id = new BlockDefinitionId("Tel.Speed"),
                PreferredSize = BlockSize.Tile1xHalf,
                Kind = BlockKind.Telemetry,
                Placement = PlacementPolicy.Free,
            },
        });

        var preferences = new UiPreferences
        {
            ShellLayout = new ShellLayoutPreferences
            {
                Grid = new ShellGridLayout
                {
                    Columns = 4,
                    Rows = 4,
                },
            },
        };

        var preferencesService = new FakePreferencesService(preferences);
        var dispatcher = new FakeCommandDispatcher();

        var viewModel = new BlockLayoutViewModel(layoutStore, catalog, dispatcher, preferencesService);

        viewModel.Blocks.Should().HaveCount(2);

        var firstTile = viewModel.Blocks[0].Tile;
        firstTile.Col = 40;
        firstTile.Row = 40;

        viewModel.UpdateViewport(new Size(400, 400));

        var standardBlock = viewModel.Blocks.Single(block => block.Definition.Id.Value == "Cmd.Start");
        standardBlock.Tile.ColSpan.Should().Be(2);
        standardBlock.Tile.RowSpan.Should().Be(2);
        standardBlock.Tile.Col.Should().BeLessOrEqualTo(viewModel.Grid.Columns - standardBlock.Tile.ColSpan);
        standardBlock.Tile.Row.Should().BeLessOrEqualTo(viewModel.Grid.Rows - standardBlock.Tile.RowSpan);

        var telemetryBlock = viewModel.Blocks.Single(block => block.Definition.Id.Value == "Tel.Speed");
        telemetryBlock.Tile.ColSpan.Should().Be(2);
        telemetryBlock.Tile.RowSpan.Should().Be(1);
        telemetryBlock.Tile.Row.Should().BeGreaterOrEqualTo(0);
        telemetryBlock.Tile.Offset.Should().Be((0, 0));
    }

    [Fact]
    public void IsLocked_TogglesFloatingBlocksAndOverlayVisibility()
    {
        var instanceId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var layoutStore = new FakeLayoutStore(new[]
        {
            new BlockInstance
            {
                InstanceId = new BlockInstanceId(instanceId),
                DefinitionId = new BlockDefinitionId("Cmd.AutoSteerToggle"),
                Region = BlockRegion.Overlay,
                Origin = BlockOrigin.Clone,
            },
        });

        var catalog = new FakeCatalog(new[]
        {
            new BlockDefinition
            {
                Id = new BlockDefinitionId("Cmd.AutoSteerToggle"),
                PreferredDock = BlockRegion.Overlay,
                Kind = BlockKind.CommandButton,
                Placement = PlacementPolicy.MenuScoped,
            },
        });

        var preferences = new UiPreferences
        {
            ShellLayout = new ShellLayoutPreferences
            {
                IsLayoutLocked = true,
                Grid = new ShellGridLayout
                {
                    FloatingBlocks = new List<FloatingBlockSpec>
                    {
                        new()
                        {
                            InstanceId = instanceId,
                            X = 48,
                            Y = 48,
                            Width = 180,
                            Height = 160,
                        },
                    },
                },
            },
        };

        var preferencesService = new FakePreferencesService(preferences);
        var dispatcher = new FakeCommandDispatcher();

        var viewModel = new BlockLayoutViewModel(layoutStore, catalog, dispatcher, preferencesService);

        viewModel.AreFloatingOverlaysVisible.Should().BeFalse();
        viewModel.FloatingBlocks.Should().ContainSingle();
        viewModel.FloatingBlocks[0].IsLocked.Should().BeTrue();

        viewModel.IsLocked = false;

        viewModel.AreFloatingOverlaysVisible.Should().BeTrue();
        viewModel.FloatingBlocks[0].IsLocked.Should().BeFalse();
    }

    [Fact]
    public void OpenSettingsCommand_RaisesFloatingBlockSettingsRequested()
    {
        var instanceId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var layoutStore = new FakeLayoutStore(new[]
        {
            new BlockInstance
            {
                InstanceId = new BlockInstanceId(instanceId),
                DefinitionId = new BlockDefinitionId("Cmd.SectionMaster"),
                Region = BlockRegion.Overlay,
                Origin = BlockOrigin.Clone,
            },
        });

        var catalog = new FakeCatalog(new[]
        {
            new BlockDefinition
            {
                Id = new BlockDefinitionId("Cmd.SectionMaster"),
                PreferredDock = BlockRegion.Overlay,
                Kind = BlockKind.CommandButton,
                Placement = PlacementPolicy.MenuScoped,
            },
        });

        var preferences = new UiPreferences
        {
            ShellLayout = new ShellLayoutPreferences
            {
                IsLayoutLocked = false,
                Grid = new ShellGridLayout
                {
                    FloatingBlocks = new List<FloatingBlockSpec>
                    {
                        new()
                        {
                            InstanceId = instanceId,
                            X = 120,
                            Y = 180,
                            Width = 200,
                            Height = 180,
                        },
                    },
                },
            },
        };

        var preferencesService = new FakePreferencesService(preferences);
        var dispatcher = new FakeCommandDispatcher();

        var viewModel = new BlockLayoutViewModel(layoutStore, catalog, dispatcher, preferencesService);
        viewModel.UpdateViewport(new Size(800, 600));

        var block = viewModel.FloatingBlocks.Should().ContainSingle().Subject;
        var invoked = false;
        FloatingBlockViewModel? observed = null;
        viewModel.FloatingBlockSettingsRequested += (_, args) =>
        {
            invoked = true;
            observed = args;
        };

        block.OpenSettingsCommand.CanExecute(null).Should().BeTrue();
        block.OpenSettingsCommand.Execute(null);

        invoked.Should().BeTrue();
        observed.Should().BeSameAs(block);
    }

    [Fact]
    public void UpdateFloatingBlock_PersistsBounds()
    {
        var instanceId = Guid.Parse("00000000-0000-0000-0000-000000000030");
        var layoutStore = new FakeLayoutStore(new[]
        {
            new BlockInstance
            {
                InstanceId = new BlockInstanceId(instanceId),
                DefinitionId = new BlockDefinitionId("Cmd.Start"),
                Region = BlockRegion.Overlay,
                Origin = BlockOrigin.Clone,
            },
        });

        var catalog = new FakeCatalog(new[]
        {
            new BlockDefinition
            {
                Id = new BlockDefinitionId("Cmd.Start"),
                PreferredDock = BlockRegion.Overlay,
                Kind = BlockKind.CommandButton,
                Placement = PlacementPolicy.Free,
            },
        });

        var preferences = new UiPreferences
        {
            ShellLayout = new ShellLayoutPreferences
            {
                IsLayoutLocked = false,
                Grid = new ShellGridLayout
                {
                    FloatingBlocks = new List<FloatingBlockSpec>
                    {
                        new()
                        {
                            InstanceId = instanceId,
                            X = 40,
                            Y = 40,
                            Width = 180,
                            Height = 160,
                        },
                    },
                },
            },
        };

        var preferencesService = new FakePreferencesService(preferences);
        var dispatcher = new FakeCommandDispatcher();

        var viewModel = new BlockLayoutViewModel(layoutStore, catalog, dispatcher, preferencesService);
        viewModel.UpdateViewport(new Size(1024, 768));

        var block = viewModel.FloatingBlocks.Should().ContainSingle().Subject;
        var target = new Rect(320, 256, 240, 200);

        viewModel.UpdateFloatingBlock(block, target);

        layoutStore.SavedInstances.Should().ContainSingle(instance => instance.InstanceId.Value == instanceId);

        var snapshot = preferencesService.GetPreferences();
        snapshot.ShellLayout.Should().NotBeNull();
        snapshot.ShellLayout!.Grid.Should().NotBeNull();
        var spec = snapshot.ShellLayout!.Grid!.FloatingBlocks.Should().ContainSingle().Subject;
        spec.X.Should().BeApproximately(block.Bounds.X, 0.1);
        spec.Y.Should().BeApproximately(block.Bounds.Y, 0.1);
        spec.Width.Should().BeApproximately(block.Bounds.Width, 0.1);
        spec.Height.Should().BeApproximately(block.Bounds.Height, 0.1);
    }

    private sealed class FakeLayoutStore : IBlockLayoutStore
    {
        private readonly List<BlockInstance> _instances;

        public IReadOnlyList<BlockInstance> SavedInstances { get; private set; } = Array.Empty<BlockInstance>();

        public FakeLayoutStore(IEnumerable<BlockInstance> instances)
        {
            _instances = instances?.Select(instance => instance.Clone()).ToList() ?? new List<BlockInstance>();
        }

        public IReadOnlyList<BlockInstance> Load() => _instances.Select(instance => instance.Clone()).ToArray();

        public void Save(IEnumerable<BlockInstance> instances)
        {
            ArgumentNullException.ThrowIfNull(instances);
            var snapshot = instances.Select(instance => instance.Clone()).ToArray();
            SavedInstances = snapshot;
            _instances.Clear();
            _instances.AddRange(snapshot.Select(instance => instance.Clone()));
        }
    }

    private sealed class FakeCatalog : IBlockCatalog
    {
        private readonly Dictionary<BlockDefinitionId, BlockDefinition> _definitions;

        public FakeCatalog(IEnumerable<BlockDefinition> definitions)
        {
            _definitions = definitions.ToDictionary(def => def.Id, def => def);
        }

        public IEnumerable<BlockDefinition> All() => _definitions.Values;

        public BlockDefinition? Get(BlockDefinitionId id) => _definitions.TryGetValue(id, out var value) ? value : null;
    }

    private sealed class FakePreferencesService : IUiPreferencesService
    {
        private UiPreferences _preferences;

        public FakePreferencesService(UiPreferences preferences)
        {
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        }

        public UiPreferences GetPreferences() => _preferences.Clone();

        public void UpdateTheme(UiTheme theme)
        {
            throw new NotSupportedException();
        }

        public void UpdateWindowPlacement(WindowPlacement placement)
        {
            throw new NotSupportedException();
        }

        public void UpdateTelemetryOptIn(bool isOptedIn)
        {
            throw new NotSupportedException();
        }

        public void UpdateRunMode(AvaloniaRunMode mode)
        {
            throw new NotSupportedException();
        }

        public void UpdateShellLayout(ShellLayoutPreferences layout)
        {
            _preferences = new UiPreferences
            {
                Theme = _preferences.Theme,
                TelemetryOptIn = _preferences.TelemetryOptIn,
                Window = _preferences.Window,
                RunMode = _preferences.RunMode,
                ShellLayout = layout.Clone(),
            };
        }
    }

    private sealed class FakeCommandDispatcher : IShellCommandDispatcher
    {
        public ValueTask<bool> DispatchAsync(string injectionPoint, string commandId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(true);
        }
    }
}
