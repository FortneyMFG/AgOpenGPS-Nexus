using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace Aog.Core.Host;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        try
        {
            using var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Core host terminated unexpectedly.");
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
                configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                configurationBuilder.AddEnvironmentVariables(prefix: "NEXUS_");
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
                    .AddOptions<CoreHealthOptions>()
                    .BindConfiguration("CoreHost:Health")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddHostedService<CoreHealthService>();
            });
}
