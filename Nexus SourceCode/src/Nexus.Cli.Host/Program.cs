using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nexus.Cli.Host;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddNxCliHost();

        await using var host = builder.Build();

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            await host.StartAsync(cancellation.Token);

            var application = host.Services.GetRequiredService<NxApplication>();
            try
            {
                return await application.InvokeAsync(args, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return 130; // 128 + SIGINT
            }
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            await host.StopAsync(CancellationToken.None);
        }

        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        }
    }
}
