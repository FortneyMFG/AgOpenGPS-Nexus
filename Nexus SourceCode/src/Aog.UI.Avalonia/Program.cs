using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Aog.Abstractions.Runtime;
using Aog.UI.Avalonia.App;
using Aog.UI.Avalonia.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        DotNetRuntimeBaseline.EnsureSupported();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables(prefix: "NEXUS_");
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
        {
            ["--runMode"] = "Avalonia:RunMode"
        });

        builder.Services
            .AddOptions<AvaloniaShellOptions>()
            .Bind(builder.Configuration.GetSection("Avalonia"))
            .ValidateDataAnnotations();

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
        AppBuilder.Configure(() => services.GetRequiredService<NexusApp>())
            .UsePlatformDetect()
            .LogToTrace();
}
