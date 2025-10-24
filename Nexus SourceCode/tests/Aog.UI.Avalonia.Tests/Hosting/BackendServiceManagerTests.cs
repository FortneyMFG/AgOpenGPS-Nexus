using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aog.UI.Avalonia.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.Hosting;

public sealed class BackendServiceManagerTests
{
    [Fact]
    public void DescriptorProjectPaths_ShouldResolveOnCurrentPlatform()
    {
        var logger = NullLogger<BackendServiceManager>.Instance;
        var manager = new BackendServiceManager(logger);

        var descriptorsField = typeof(BackendServiceManager).GetField(
            "_descriptors",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(descriptorsField);
        var descriptors = (IDictionary?)descriptorsField!.GetValue(manager);
        Assert.NotNull(descriptors);

        foreach (DictionaryEntry entry in descriptors!)
        {
            var descriptor = entry.Value;
            Assert.NotNull(descriptor);

            var projectPathProperty = descriptor!.GetType().GetProperty(
                "ProjectPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(projectPathProperty);

            var projectPath = projectPathProperty!.GetValue(descriptor) as string;
            Assert.False(string.IsNullOrWhiteSpace(projectPath));

            Assert.True(File.Exists(projectPath!), $"Project path '{projectPath}' should exist.");

            var workingDirectory = Path.GetDirectoryName(projectPath!);
            Assert.False(string.IsNullOrWhiteSpace(workingDirectory));
            Assert.True(Directory.Exists(workingDirectory!),
                $"Working directory '{workingDirectory}' should exist.");
        }
    }
}
