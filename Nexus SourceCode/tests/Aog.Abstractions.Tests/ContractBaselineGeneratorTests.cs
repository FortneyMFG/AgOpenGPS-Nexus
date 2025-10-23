using Aog.Abstractions.Contracts;
using Google.Protobuf;
using Xunit;

namespace Aog.Abstractions.Tests
{
    public class ContractBaselineGeneratorTests
    {
        [Fact]
        public void GenerateBaseline()
        {
            var currentDescriptor = GrpcContractRegistry.DescriptorSet;
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);
            currentDescriptor.WriteTo(output);
            output.Flush();
            var bytes = Convert.ToBase64String(stream.ToArray());
            var baselinePath = Path.Combine(GetSourceRoot(), "tests", "Aog.Abstractions.Tests", "Baselines", "aog-contracts.desc.base64");
            File.WriteAllText(baselinePath, bytes);
        }

        private static string GetSourceRoot()
        {
            var path = AppContext.BaseDirectory;
            while (!Directory.Exists(Path.Combine(path, ".git")) && Path.GetDirectoryName(path) != null)
            {
                path = Path.GetDirectoryName(path)!;
            }

            if (path == null || !Directory.Exists(Path.Combine(path, ".git")))
            {
                throw new InvalidOperationException("Unable to locate repository root.");
            }

            return path;
        }
    }
}
