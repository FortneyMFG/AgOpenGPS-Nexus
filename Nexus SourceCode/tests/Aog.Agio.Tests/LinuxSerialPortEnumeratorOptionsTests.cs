using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aog.Agio.Linux.Serial;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LinuxSerialPortEnumeratorOptionsTests
{
    [Fact]
    public void DevicePrefixes_NormalizesWhitespaceAndRejectsNonRootedPaths()
    {
        var options = new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[] { "  /dev/ttyUSB  " },
        };

        Assert.Equal(new[] { "/dev/ttyUSB" }, options.DevicePrefixes);
    }

    [Fact]
    public void DevicePrefixes_LogsAndDropsRelativeEntries()
    {
        var listener = new CollectingTraceListener();
        Trace.Listeners.Add(listener);

        try
        {
            var options = new LinuxSerialPortEnumeratorOptions
            {
                DevicePrefixes = new[] { "/dev/ttyACM", "relative/path" },
            };

            Assert.Equal(new[] { "/dev/ttyACM" }, options.DevicePrefixes);
            Assert.Contains(listener.Messages, message => message.Contains("relative/path", StringComparison.Ordinal));
        }
        finally
        {
            Trace.Listeners.Remove(listener);
            listener.Dispose();
        }
    }

    private sealed class CollectingTraceListener : TraceListener
    {
        public List<string> Messages { get; } = new();

        public override void Write(string? message)
        {
            if (message is not null)
            {
                Messages.Add(message);
            }
        }

        public override void WriteLine(string? message)
        {
            if (message is not null)
            {
                Messages.Add(message);
            }
        }
    }
}
