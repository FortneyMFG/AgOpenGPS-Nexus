namespace Nexus.Cli.Host.Core.Endpoints;

public sealed record CoreEndpoint(CoreTransportKind Transport, string Address, string Source)
{
    public override string ToString() => Address;
}
