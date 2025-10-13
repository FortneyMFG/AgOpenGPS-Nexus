using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Core.Replay;

internal sealed record class ReplayFrame(
    TimeSpan Offset,
    DateTime? TimestampUtc,
    Func<IEventBus, CancellationToken, ValueTask> PublishAsync);
