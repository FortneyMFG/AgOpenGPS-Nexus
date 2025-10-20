# Creating Your First Plugin

This tutorial walks through creating a basic Nexus plugin. For detailed requirements, see [ADR-018: Plugin API](../../ADR/ADR-018-plugin-api.md) and [Plugin Requirements](../../SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md).

## Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Nexus development environment

## Project Setup

1. **Create Project**
```powershell
dotnet new nexus-plugin -n MyFirstPlugin
```

2. **Project Structure**
```
MyFirstPlugin/
├── src/
│   ├── MyFirstPlugin.csproj
│   ├── Plugin.cs
│   └── Services/
├── test/
│   └── MyFirstPlugin.Tests.csproj
└── plugin.manifest.json
```

## Basic Plugin

### Plugin Manifest
```json
{
  "id": "com.example.myfirstplugin",
  "version": "1.0.0",
  "name": "My First Plugin",
  "description": "Example Nexus plugin",
  "capabilities": {
    "required": [
      "pose.read",
      "map.write"
    ]
  },
  "dependencies": {
    "Aog.Core": "^1.0.0"
  }
}
```

### Plugin Implementation
```csharp
using Aog.Plugin;
using Aog.Core.Contracts;

namespace MyFirstPlugin
{
    public class Plugin : IPlugin
    {
        private readonly IPoseStream _poseStream;
        private readonly IMapService _mapService;

        public Plugin(IPoseStream poseStream, IMapService mapService)
        {
            _poseStream = poseStream;
            _mapService = mapService;
        }

        public Task InitializeAsync(IPluginContext context)
        {
            // Setup plugin
            return Task.CompletedTask;
        }

        public Task StartAsync()
        {
            // Start plugin operations
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            // Cleanup
            return Task.CompletedTask;
        }
    }
}
```

## Adding Features

### 1. Subscribe to PoseStream
```csharp
private async Task HandlePoseUpdate(PoseStreamFrame frame)
{
    // Process pose update
    var position = frame.Position;
    var heading = frame.Heading;
    
    // Update map or state
    await _mapService.UpdatePositionAsync(position);
}
```

### 2. Create Map Layer
```csharp
public async Task CreateCustomLayer()
{
    var layer = new MapLayer
    {
        Id = "custom-coverage",
        Name = "Custom Coverage",
        Type = LayerType.Coverage,
        Properties = new LayerProperties
        {
            Color = "#FF4081",
            Opacity = 0.6
        }
    };

    await _mapService.CreateLayerAsync(layer);
}
```

### 3. Add Settings
```csharp
public class PluginSettings
{
    public string LayerName { get; set; }
    public double UpdateInterval { get; set; }
    public bool EnableLogging { get; set; }
}

// Load settings
var settings = context.Configuration.Get<PluginSettings>();
```

## Testing

### Unit Tests
```csharp
[Fact]
public async Task HandlePoseUpdate_UpdatesMap()
{
    // Arrange
    var mockPoseStream = new Mock<IPoseStream>();
    var mockMapService = new Mock<IMapService>();
    var plugin = new Plugin(mockPoseStream.Object, mockMapService.Object);

    // Act
    await plugin.HandlePoseUpdate(testFrame);

    // Assert
    mockMapService.Verify(m => m.UpdatePositionAsync(It.IsAny<Position>()));
}
```

### Integration Tests
```csharp
[Fact]
public async Task Plugin_IntegratesWithCore()
{
    // Arrange
    using var testHarness = await TestHarness.CreateAsync();
    var plugin = await testHarness.LoadPluginAsync<Plugin>();

    // Act
    await plugin.StartAsync();
    await testHarness.SimulateFramesAsync(testData);

    // Assert
    var layer = await testHarness.GetLayerAsync("custom-coverage");
    Assert.NotNull(layer);
}
```

## Deployment

### 1. Build Plugin
```powershell
dotnet build -c Release
```

### 2. Package Plugin
```powershell
nexus-plugin pack ./bin/Release
```

### 3. Install Plugin
```powershell
nexus-plugin install ./MyFirstPlugin.nxpkg
```

## Best Practices

1. **Error Handling**
   - Use try-catch blocks
   - Log errors appropriately
   - Implement graceful degradation

2. **Resource Management**
   - Dispose of resources
   - Unsubscribe from events
   - Clean up on shutdown

3. **Performance**
   - Minimize allocations
   - Use async operations
   - Profile critical paths

## Debugging

### Local Debugging
1. Start Nexus in debug mode
2. Attach to plugin process
3. Use logging and breakpoints

### Remote Debugging
1. Enable remote debugging
2. Connect to remote instance
3. Use diagnostic tools

## Related Documentation

- [Plugin API Reference](../reference/api.md)
- [Plugin Security Guide](../security.md)
- [Testing Guidelines](../../development/testing.md)
- [Performance Guide](../../development/performance.md)
