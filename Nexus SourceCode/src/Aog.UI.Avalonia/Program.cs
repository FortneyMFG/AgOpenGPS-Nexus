using System;
using System.Threading;
using Avalonia;
using Aog.UI.Avalonia.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddAvaloniaUiShell();
        builder.Logging.AddDebug();

        using var host = builder.Build();
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        try
        {
            return BuildAvaloniaApp(host.Services).StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider services) =>
        AppBuilder.Configure(() => services.GetRequiredService<App>())
            .UsePlatformDetect()
            .LogToTrace();
}
