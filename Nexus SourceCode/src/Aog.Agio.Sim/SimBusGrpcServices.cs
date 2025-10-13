using System;
using System.Threading.Tasks;
using Aog.Protos.Agio.V1;
using Aog.Core.V1;
using Aog.Core.Simulation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Aog.Agio.Sim;

/// <summary>
/// Streams simulated GNSS pose updates over gRPC.
/// </summary>
public sealed class SimGnssService : GnssService.GnssServiceBase
{
    private readonly ISimBus _simBus;

    public SimGnssService(ISimBus simBus)
    {
        _simBus = simBus ?? throw new ArgumentNullException(nameof(simBus));
    }

    public override Task SubscribePose(Empty request, IServerStreamWriter<Pose> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return SimBusStreamForwarder.StreamAsync(_simBus, SimBusTopics.Pose, responseStream, context);
    }
}

/// <summary>
/// Streams simulated IMU updates over gRPC.
/// </summary>
public sealed class SimImuService : ImuService.ImuServiceBase
{
    private readonly ISimBus _simBus;

    public SimImuService(ISimBus simBus)
    {
        _simBus = simBus ?? throw new ArgumentNullException(nameof(simBus));
    }

    public override Task SubscribeImu(Empty request, IServerStreamWriter<Imu> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return SimBusStreamForwarder.StreamAsync(_simBus, SimBusTopics.Imu, responseStream, context);
    }
}

/// <summary>
/// Streams simulated steer state updates over gRPC.
/// </summary>
public sealed class SimSteerService : SteerService.SteerServiceBase
{
    private readonly ISimBus _simBus;

    public SimSteerService(ISimBus simBus)
    {
        _simBus = simBus ?? throw new ArgumentNullException(nameof(simBus));
    }

    public override Task SubscribeSteerState(Empty request, IServerStreamWriter<SteerState> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return SimBusStreamForwarder.StreamAsync(_simBus, SimBusTopics.SteerState, responseStream, context);
    }
}

/// <summary>
/// Streams simulated section mask updates over gRPC.
/// </summary>
public sealed class SimSectionsService : SectionsService.SectionsServiceBase
{
    private readonly ISimBus _simBus;

    public SimSectionsService(ISimBus simBus)
    {
        _simBus = simBus ?? throw new ArgumentNullException(nameof(simBus));
    }

    public override Task SubscribeSectionMask(Empty request, IServerStreamWriter<SectionMask> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return SimBusStreamForwarder.StreamAsync(_simBus, SimBusTopics.SectionMask, responseStream, context);
    }
}

/// <summary>
/// Streams simulated CAN frames over gRPC.
/// </summary>
public sealed class SimCanBusService : CanBusService.CanBusServiceBase
{
    private readonly ISimBus _simBus;

    public SimCanBusService(ISimBus simBus)
    {
        _simBus = simBus ?? throw new ArgumentNullException(nameof(simBus));
    }

    public override Task SubscribeFrames(Empty request, IServerStreamWriter<CanFrame> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return SimBusStreamForwarder.StreamAsync(_simBus, SimBusTopics.CanFrame, responseStream, context);
    }
}

/// <summary>
/// Streams simulated timing capability updates over gRPC.
/// </summary>
public sealed class SimTimingService : TimingService.TimingServiceBase
{
    private readonly ISimBus _simBus;

    public SimTimingService(ISimBus simBus)
    {
        _simBus = simBus ?? throw new ArgumentNullException(nameof(simBus));
    }

    public override Task SubscribeTiming(Empty request, IServerStreamWriter<TimingCaps> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return SimBusStreamForwarder.StreamAsync(_simBus, SimBusTopics.Timing, responseStream, context);
    }
}
