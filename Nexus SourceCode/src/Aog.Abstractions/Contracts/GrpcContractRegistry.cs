using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aog.Core.V1;
using Aog.Guidance.V1;
using Aog.Protos.Agio.V1;
using Aog.Protos.Capabilities.V1;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Aog.Abstractions.Contracts;

/// <summary>
/// Computes stable fingerprints for the protobuf contracts shipped with Nexus.
/// </summary>
public static class GrpcContractRegistry
{
    /// <summary>
    /// gRPC metadata header used to advertise the descriptor fingerprint during handshakes.
    /// </summary>
    public const string FingerprintHeaderName = "nexus-contract-sha256";

    /// <summary>
    /// Capability attribute key attached to advertised capabilities so remote peers can
    /// verify they are speaking with the expected contract set.
    /// </summary>
    public const string FingerprintCapabilityAttribute = "nexus.grpc.contracts.sha256";

    private static readonly Lazy<string> LazyFingerprint = new(ComputeFingerprint);
    private static readonly Lazy<FileDescriptorSet> LazyDescriptorSet = new(BuildDescriptorSet);

    /// <summary>
    /// Gets the SHA-256 fingerprint of the bundled protobuf descriptor set.
    /// </summary>
    public static string DescriptorFingerprint => LazyFingerprint.Value;

    /// <summary>
    /// Gets the protobuf descriptor set covering all Nexus gRPC contracts.
    /// </summary>
    public static FileDescriptorSet DescriptorSet => LazyDescriptorSet.Value;

    private static string ComputeFingerprint()
    {
        var descriptorBytes = DescriptorSet.ToByteArray();
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(descriptorBytes);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            _ = builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    private static FileDescriptorSet BuildDescriptorSet()
    {
        var roots = new[]
        {
            CoreReflection.Descriptor,
            CapabilitiesReflection.Descriptor,
            AgioSimReflection.Descriptor,
            GuidanceReflection.Descriptor,
        };

        var queue = new Stack<FileDescriptor>(roots);
        var visited = new Dictionary<string, FileDescriptor>(StringComparer.Ordinal);

        while (queue.Count > 0)
        {
            var descriptor = queue.Pop();
            if (!visited.TryAdd(descriptor.Name, descriptor))
            {
                continue;
            }

            foreach (var dependency in descriptor.Dependencies)
            {
                queue.Push(dependency);
            }
        }

        var set = new FileDescriptorSet();
        foreach (var file in visited.Values.OrderBy(d => d.Name, StringComparer.Ordinal))
        {
            set.File.Add(file.Proto);
        }

        return set;
    }
}
