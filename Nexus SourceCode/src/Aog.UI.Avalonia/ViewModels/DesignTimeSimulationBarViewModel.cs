using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Design-time variant of <see cref="SimulationBarViewModel"/> used by the Avalonia designer.
/// </summary>
public sealed class DesignTimeSimulationBarViewModel : SimulationBarViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeSimulationBarViewModel"/> class.
    /// </summary>
    public DesignTimeSimulationBarViewModel()
        : base(CreateConfiguration(), new DesignTimeReplayController(), NullLogger<SimulationBarViewModel>.Instance)
    {
    }

    private static SimulationConfiguration CreateConfiguration()
    {
        const string json = """
{
  "schemaVersion": "1.0.0",
  "providers": [
    { "providerId": "sim.clock.fixed", "outputs": ["time"] },
    { "providerId": "sim.vehicle.bicycle", "inputs": ["time"], "outputs": ["pose"] },
    { "providerId": "sim.imu.synthetic", "inputs": ["time", "pose"], "outputs": ["imu"] }
  ],
  "routes": [
    { "stream": "pose", "source": "sim.vehicle.bicycle", "mode": "simulation" },
    { "stream": "imu", "source": "sim.imu.synthetic", "mode": "simulation" }
  ],
  "options": { "seed": 2024, "timeScale": 1.0 },
  "scenarios": []
}
""";

        return SimulationConfigurationLoader.Load(json);
    }

    private sealed class DesignTimeReplayController : IReplayController
    {
        private ReplayState _state = new(false, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), 1.0);

        public event EventHandler<ReplayStateChangedEventArgs>? StateChanged;

        public ReplayState State => _state;

        public ValueTask PlayAsync(CancellationToken cancellationToken = default)
        {
            UpdateState(_state with { IsPlaying = true });
            return ValueTask.CompletedTask;
        }

        public ValueTask PauseAsync(CancellationToken cancellationToken = default)
        {
            UpdateState(_state with { IsPlaying = false });
            return ValueTask.CompletedTask;
        }

        public ValueTask SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        {
            var clamped = position < TimeSpan.Zero
                ? TimeSpan.Zero
                : position > _state.Duration
                    ? _state.Duration
                    : position;

            UpdateState(_state with { Position = clamped });
            return ValueTask.CompletedTask;
        }

        public ValueTask SetPlaybackRateAsync(double playbackRate, CancellationToken cancellationToken = default)
        {
            var rate = playbackRate > 0 ? playbackRate : _state.PlaybackRate;
            UpdateState(new ReplayState(_state.IsPlaying, _state.Position, _state.Duration, rate));
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            StateChanged = null;
            return ValueTask.CompletedTask;
        }

        private void UpdateState(ReplayState next)
        {
            if (next.Equals(_state))
            {
                return;
            }

            _state = next;
            StateChanged?.Invoke(this, new ReplayStateChangedEventArgs(_state));
        }
    }
}
