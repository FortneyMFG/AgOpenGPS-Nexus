namespace Aog.Bridge.Host;

public interface IAogLinkGateway
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
