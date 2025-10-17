using Aog.Abstractions.Runtime;
using Aog.Agio.AogLink;
using Aog.Agio.Legacy;
using Aog.Agio.RadioBridge;
using Aog.Agio.Safety;
using Aog.Agio.Telemetry;
using Aog.Agio.Timing;
using Aog.Core.Mesh;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace Aog.Agio;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        DotNetRuntimeBaseline.EnsureSupported();

        try
        {
            using var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "AGiO host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[]? args = null) =>
        GenericHost.CreateDefaultBuilder(args ?? Array.Empty<string>())
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                Microsoft.Extensions.Configuration.JsonConfigurationExtensions.AddJsonFile(
                    configurationBuilder,
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: true);
                Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions.AddEnvironmentVariables(
                    configurationBuilder,
                    prefix: "NEXUS_");
            })
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            })
            .ConfigureServices((context, services) =>
            {
                services
                    .AddOptions<AgioHostOptions>()
                    .BindConfiguration("AgioHost")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<AgioSafetyOptions>()
                    .BindConfiguration("AgioHost:Safety")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<SafetyLogOptions>()
                    .BindConfiguration("AgioHost:SafetyLogs")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<AogLinkTransportOptions>()
                    .BindConfiguration("AgioHost:AogLink")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<RadioBridgeElrsAdapterOptions>()
                    .BindConfiguration("AgioHost:RadioBridge:Elrs")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<RadioBridgeLoraAdapterOptions>()
                    .BindConfiguration("AgioHost:RadioBridge:Lora")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<MeshTelemetryAggregatorOptions>()
                    .BindConfiguration("AgioHost:Mesh")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<LegacyMeshOptions>()
                    .BindConfiguration("AgioHost:LegacyMesh")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<IActuatorFailsafeService, ActuatorFailsafeService>();
                services.AddSingleton<ISafetyLog, FileSafetyLog>();

                services.AddSingleton<LiveTelemetryMeshService>();
                services.AddSingleton<ILiveTelemetryMeshService>(provider => provider.GetRequiredService<LiveTelemetryMeshService>());
                services.AddSingleton<MeshTelemetryAggregator>();
                services.AddSingleton<IRadioBridgeLinkFactory, RadioBridgeLinkFactory>();
                services.AddSingleton<RadioBridgeElrsAdapter>();
                services.AddSingleton<RadioBridgeLoraAdapter>();

                if (OperatingSystem.IsLinux())
                {
                    services.AddSingleton<ITimingCapabilitiesProbe, LinuxTimingCapabilitiesProbe>();
                }
                else
                {
                    services.AddSingleton<ITimingCapabilitiesProbe, NullTimingCapabilitiesProbe>();
                }

                services.AddHostedService<TimingCapabilitiesLoggerService>();

                services.AddSingleton<IAogLinkTransportDriver, EthernetAogLinkTransportDriver>();
                services.AddSingleton<IAogLinkTransportDriver, SerialAogLinkTransportDriver>();
                services.AddSingleton<IAogLinkTransportDriver, CanAogLinkTransportDriver>();
                services.AddSingleton<AogLinkBridge>();
                services.AddSingleton<AogLinkTransportManager>();
                services.AddHostedService(provider => provider.GetRequiredService<AogLinkTransportManager>());

                var backendOptions = context.Configuration
                    .GetSection("AgioHost:Backend")
                    .Get<AgioHostOptions.BackendOptions>() ?? new AgioHostOptions.BackendOptions();

                var backend = AgioBackendLoader.Load(backendOptions);
                backend.ConfigureServices(services);

                services.AddSingleton<ILegacyPoseObserver>(provider => provider.GetRequiredService<MeshTelemetryAggregator>());
                services.AddHostedService(provider => provider.GetRequiredService<RadioBridgeElrsAdapter>());
                services.AddHostedService(provider => provider.GetRequiredService<RadioBridgeLoraAdapter>());

                services.AddSingleton(backend);
                services.AddSingleton(new AgioBackendRegistration(
                    backendOptions.Assembly,
                    backendOptions.Type,
                    backend.GetType(),
                    backend.Name));

                services.AddHostedService<AgioBackendHostedService>();
            });
}
