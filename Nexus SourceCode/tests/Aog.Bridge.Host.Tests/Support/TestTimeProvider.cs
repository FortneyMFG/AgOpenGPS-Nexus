using System;

namespace Aog.Bridge.Host.Tests.Support;

internal sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;
    private long _timestamp;

    public TestTimeProvider(DateTimeOffset? initial = null)
    {
        _utcNow = initial ?? DateTimeOffset.UtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override long GetTimestamp() => _timestamp;

    public override TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
    {
        return TimeSpan.FromMilliseconds(endingTimestamp - startingTimestamp);
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow += delta;
        _timestamp += (long)delta.TotalMilliseconds;
    }
}
