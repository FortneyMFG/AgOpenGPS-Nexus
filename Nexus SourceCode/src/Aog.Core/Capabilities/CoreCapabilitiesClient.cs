using System;
using System.Collections.Generic;
using Aog.Protos.Capabilities.V1;

namespace Aog.Core.Capabilities;

/// <summary>
/// Builds <see cref="HandshakeRequest"/> messages for the Core host.
/// </summary>
public sealed class CoreCapabilitiesClient
{
    private readonly CapabilityDescriptorFactory _factory;

    public CoreCapabilitiesClient(CapabilityDescriptorFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Creates a handshake request that advertises the provided capabilities.
    /// </summary>
    public HandshakeRequest BuildHandshake(string nodeId, IEnumerable<string> capabilityNames, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node identifier is required.", nameof(nodeId));
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session identifier is required.", nameof(sessionId));
        }

        if (capabilityNames is null)
        {
            throw new ArgumentNullException(nameof(capabilityNames));
        }

        var request = new HandshakeRequest
        {
            SessionId = sessionId,
            NodeId = nodeId,
            Role = CapabilityRole.CapabilityRoleCore
        };

        var descriptors = _factory.Create(capabilityNames);
        request.Capabilities.Add(descriptors);

        return request;
    }
}

/// <summary>
/// Produces <see cref="CapabilityDescriptor"/> instances with consistent metadata.
/// </summary>
public sealed class CapabilityDescriptorFactory
{
    private readonly string? _defaultVersion;
    private readonly string? _defaultSummary;
    private readonly IReadOnlyDictionary<string, string> _defaultAttributes;

    public CapabilityDescriptorFactory(
        string? defaultVersion = null,
        string? defaultSummary = null,
        IReadOnlyDictionary<string, string>? defaultAttributes = null)
    {
        _defaultVersion = defaultVersion;
        _defaultSummary = defaultSummary;
        _defaultAttributes = defaultAttributes ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Generates descriptors for the provided capability names, skipping empty entries
    /// and de-duplicating on the canonical name.
    /// </summary>
    public IEnumerable<CapabilityDescriptor> Create(IEnumerable<string> capabilityNames)
    {
        if (capabilityNames is null)
        {
            throw new ArgumentNullException(nameof(capabilityNames));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in capabilityNames)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed) || !seen.Add(trimmed))
            {
                continue;
            }

            var descriptor = new CapabilityDescriptor
            {
                Name = trimmed,
            };

            if (!string.IsNullOrWhiteSpace(_defaultVersion))
            {
                descriptor.Version = _defaultVersion;
            }

            if (!string.IsNullOrWhiteSpace(_defaultSummary))
            {
                descriptor.Summary = _defaultSummary;
            }

            foreach (var kvp in _defaultAttributes)
            {
                descriptor.Attributes[kvp.Key] = kvp.Value;
            }

            yield return descriptor;
        }
    }
}
