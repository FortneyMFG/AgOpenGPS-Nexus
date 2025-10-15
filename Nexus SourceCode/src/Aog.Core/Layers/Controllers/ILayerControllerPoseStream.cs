using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Provides access to PoseStream samples for layer controller ingestion pipelines.
/// </summary>
public interface ILayerControllerPoseStream
{
    /// <summary>
    /// Subscribes to PoseStream frames. Handlers are invoked sequentially for each pose.
    /// </summary>
    /// <param name="handler">Handler invoked for each pose frame.</param>
    /// <returns>A disposable subscription.</returns>
    IDisposable Subscribe(Func<LayerControllerPoseFrame, CancellationToken, ValueTask> handler);

    /// <summary>
    /// Attempts to retrieve the latest pose frame.
    /// </summary>
    /// <param name="frame">Latest frame when available.</param>
    /// <returns><c>true</c> when a frame has been observed.</returns>
    bool TryGetLatestFrame(out LayerControllerPoseFrame frame);
}
