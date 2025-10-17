using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aog.Abstractions.Contracts;
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
        var current = GrpcContractRegistry.DescriptorSet;

        Assert.Equal(baseline.ToByteArray(), current.ToByteArray());
    }

    private static string GetSourceRoot()
    {
        var path = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(path))
        {
            if (File.Exists(Path.Combine(path, "Nexus.sln")))
            {
                return path;
            }

            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent) || string.Equals(parent, path, StringComparison.Ordinal))
            {
                break;
            }

            path = parent;
        }

        throw new InvalidOperationException("Failed to resolve source root.");
    }
}
