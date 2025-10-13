using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aog.Protos.Capabilities.V1;
using Grpc.Core;

namespace Aog.Agio.Capabilities;

/// <summary>
/// Minimal placeholder implementation of the Capabilities handshake on the AGiO host.
/// </summary>
public sealed class AgioCapabilitiesService : CapabilitiesService.CapabilitiesServiceBase
{
    private readonly string _nodeId;
    private readonly IReadOnlyList<CapabilityDescriptor> _capabilities;
    private readonly HashSet<string> _capabilityNames;

    public AgioCapabilitiesService(string nodeId, IEnumerable<CapabilityDescriptor> capabilities)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node identifier is required.", nameof(nodeId));
        }

        if (capabilities is null)
        {
            throw new ArgumentNullException(nameof(capabilities));
        }

        _nodeId = nodeId;
        _capabilities = capabilities.Select(CloneValidated).ToList();
        _capabilityNames = _capabilities.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public override Task<HandshakeResponse> Handshake(HandshakeRequest request, ServerCallContext context)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = new HandshakeResponse
        {
            SessionId = request.SessionId,
            NodeId = _nodeId,
            Role = CapabilityRole.CapabilityRoleAgio,
        };

        response.AcceptedCapabilities.Add(_capabilities.Select(c => c.Clone()));

        foreach (var capability in request.Capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability.Name))
            {
                continue;
            }

            if (_capabilityNames.Contains(capability.Name))
            {
                continue;
            }

            response.Rejections.Add(new CapabilityRejection
            {
                Capability = capability.Clone(),
                Reason = "Capability not supported by AGiO host.",
            });
        }

        return Task.FromResult(response);
    }

    private static CapabilityDescriptor CloneValidated(CapabilityDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentException("Capabilities cannot contain null entries.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            throw new ArgumentException("Capabilities must include a name.", nameof(descriptor));
        }

        return descriptor.Clone();
    }
}
