using System.Linq;
using Aog.UI.Avalonia;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Hosting;

public class AvaloniaUiShellServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAvaloniaUiShell_RegistersCoreTypesAsSingletons()
    {
        var services = new ServiceCollection();

        services.AddAvaloniaUiShell();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(App))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(MainWindow))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(MainWindowViewModel))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ConnectionSettingsViewModel))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IConnectionSettingsStore)
            && descriptor.ImplementationType == typeof(JsonConnectionSettingsStore))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IRunModePlatform))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IAvaloniaRunModeService))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddAvaloniaUiShell_DoesNotRegisterDuplicates()
    {
        var services = new ServiceCollection();

        services.AddAvaloniaUiShell();
        services.AddAvaloniaUiShell();

        services.Count(descriptor => descriptor.ServiceType == typeof(App)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(MainWindow)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(MainWindowViewModel)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(ConnectionSettingsViewModel)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(IConnectionSettingsStore)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(IRunModePlatform)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(IAvaloniaRunModeService)).Should().Be(1);
    }
}
