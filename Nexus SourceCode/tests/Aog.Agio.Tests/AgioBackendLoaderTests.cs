using System;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class AgioBackendLoaderTests
{
    [Fact]
    public void Load_Returns_Simulation_Backend_By_Default()
    {
        var options = new AgioHostOptions.BackendOptions();

        var backend = AgioBackendLoader.Load(options);

        Assert.NotNull(backend);
        Assert.Equal("Simulation", backend.Name);
        Assert.Equal("Aog.Agio.Sim.SimAgioBackend", backend.GetType().FullName);
    }

    [Fact]
    public void Load_Throws_When_Type_Does_Not_Implement_Interface()
    {
        var options = new AgioHostOptions.BackendOptions
        {
            Assembly = typeof(string).Assembly.GetName().Name!,
            Type = typeof(string).FullName!,
        };

        var exception = Assert.Throws<InvalidOperationException>(() => AgioBackendLoader.Load(options));
        Assert.Contains("does not implement", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
