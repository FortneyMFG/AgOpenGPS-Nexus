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
using System.Threading.Tasks;

namespace Aog.UI.Avalonia;

public static class Program
{
    private static IServiceProvider? _serviceProvider;

    [STAThread]
    public static int Main(string[] args)
    {
        DotNetRuntimeBaseline.EnsureSupported();
        InstallGlobalExceptionHandlers();

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
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
        builder.Logging.AddDebug();

        using var host = builder.Build();
        _serviceProvider = host.Services;
        AvaloniaServiceProviderAccessor.Initialize(host.Services);
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        var pluginBootstrapper = host.Services.GetRequiredService<Aog.UI.Avalonia.Plugins.PluginBootstrapper>();

        try
        {
            EnsureGraphicalEnvironment();

            return BuildAvaloniaApp(host.Services)
                .AfterSetup(_ =>
                {
                    Console.WriteLine("Starting Avalonia UI shell...");
                    try
                    {
                        pluginBootstrapper.LoadAsync().GetAwaiter().GetResult();
                        Console.WriteLine("Plugins bootstrapped.");
                    }
                    catch (Exception ex)
                    {
                        LogFatal("Plugin bootstrap failed.", ex);
                        throw;
                    }
                })
                .StartWithClassicDesktopLifetime(args);
        }
        catch (DisplayUnavailableException ex)
        {
            LogFatal("Avalonia UI cannot start because a graphical display server was not found.", ex);
            Console.Error.WriteLine(ex.Message);
            return -1;
        }
        catch (Exception ex)
        {
            LogFatal("Avalonia host terminated unexpectedly.", ex);
            return -1;
        }
        finally
        {
            pluginBootstrapper.DisposeAsync().AsTask().GetAwaiter().GetResult();
            host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider services) =>
        AppBuilder.Configure(() => services.GetRequiredService<NexusApp>())
            .UsePlatformDetect()
            .LogToTrace();

    private static void EnsureGraphicalEnvironment()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var hasDisplay = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

        if (hasDisplay)
        {
            return;
        }

        const string message = "No graphical display server was detected. Set the DISPLAY or WAYLAND_DISPLAY environment variable or run Nexus inside an X11/Wayland session.";
        throw new DisplayUnavailableException(message);
    }

    private sealed class DisplayUnavailableException : InvalidOperationException
    {
        public DisplayUnavailableException(string message)
            : base(message)
        {
        }
    }

    private static void InstallGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                LogFatal("Unhandled exception on domain boundary.", ex);
            }
            else
            {
                Console.Error.WriteLine("Unhandled non-exception error: " + args.ExceptionObject);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogFatal("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };
    }

    private static void LogFatal(string message, Exception ex)
    {
        try
        {
            if (_serviceProvider is not null)
            {
                var loggerFactory = _serviceProvider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                var logger = loggerFactory?.CreateLogger("Program");
                logger?.LogCritical(ex, message);
            }
        }
        catch
        {
            // ignore logging failures
        }

        Console.Error.WriteLine($"{message} {ex}");
    }
}
