using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aog.Core.V1;
using Aog.Protos.Agio.V1;
using Aog.Protos.Capabilities.V1;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Xunit;

namespace Aog.Abstractions.Tests;

public class ContractCompatibilityTests
{
    [Fact]
    public void ProtobufContractsMatchBaseline()
    {
        var sourceRoot = GetSourceRoot();
        var baselinePath = Path.Combine(sourceRoot, "tests", "Aog.Abstractions.Tests", "Baselines", "aog-contracts.desc.base64");
        Assert.True(File.Exists(baselinePath), $"Baseline descriptor set missing at '{baselinePath}'.");

        var baselineBytes = Convert.FromBase64String(File.ReadAllText(baselinePath));
        var baseline = FileDescriptorSet.Parser.ParseFrom(baselineBytes);
        var current = BuildDescriptorSet(new[]
        {
            CoreReflection.Descriptor,
            CapabilitiesReflection.Descriptor,
            AgioSimReflection.Descriptor
        });

        Assert.Equal(baseline.ToByteArray(), current.ToByteArray());
    }

    private static FileDescriptorSet BuildDescriptorSet(IEnumerable<FileDescriptor> roots)
    {
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
        foreach (var file in visited.Values.OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            set.File.Add(file.Proto);
        }

        return set;
    }

    private static string GetSourceRoot()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 5; i++)
        {
            path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Failed to resolve source root.");
        }

        return path;
    }
}
