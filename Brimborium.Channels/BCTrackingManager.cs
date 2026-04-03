#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

/// <summary>
/// Coordinates in-flight tracking units for a tracking processor.
/// Tracks active <see cref="IBCTracking"/> instances by id and delays the downstream
/// <c>OnComplete</c> signal until all registered trackings have finished.
/// </summary>
public interface IBCTrackingManager : IBCPart, IBCConsumer {
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="tracking"></param>
    void Add(IBCTracking tracking);

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="tracking"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task OnTrackingComplete(IBCTracking tracking, CancellationToken cancellationToken);

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="tracking"></param>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task OnTrackingError(IBCTracking tracking, BCError value, CancellationToken cancellationToken);

}

/// <summary>
/// Concrete implementation of <see cref="IBCTrackingManager"/>.
/// Uses a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> to track
/// in-flight <see cref="IBCTracking"/> units by id and forwards <c>OnComplete</c> downstream
/// only when all active trackings have reported back and the upstream has completed.
/// </summary>
public class BCTrackingManager
    : BCPartMonitored
    , IBCTrackingManager {
    private readonly ConcurrentDictionary<long, IBCTracking> _Tracking = new();
    private readonly TaskCompletionSource _Completion = new();
    private readonly IBCConsumer _NextConsumer;

    public BCTrackingManager(
            BCDescription description,
            IBCConsumer nextConsumer
        ) : base(
            description
        ) {
        this._NextConsumer = nextConsumer;
    }

    public void Add(IBCTracking tracking) {
        var id = tracking.GetId();
        if (this._Tracking.TryAdd(id, tracking)) {
            return;
        } else {
            throw new ArgumentException("a tracking with the same id is already present");
        }
    }

    // called from left
    public async Task OnComplete(CancellationToken cancellationToken) {
        if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
            if (this._Tracking.IsEmpty) {
                if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                    this._Completion.SetResult();
                    await this._NextConsumer.OnComplete(cancellationToken);
                }
            }
        }
    }

    // called from left
    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        await this._NextConsumer.OnError(value, cancellationToken);
    }

    // call from down
    public async Task OnTrackingComplete(IBCTracking tracking, CancellationToken cancellationToken) {
        if (this._Tracking.TryRemove(tracking.GetId(), out _)) {
            if (BCLifeTime.Completing == this._LifeTime) {
                if (this._Tracking.IsEmpty) {
                    if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                        this._Completion.SetResult();
                        await this._NextConsumer.OnComplete(cancellationToken);
                    }
                }
            }
        }
    }

    // call from down
    public async Task OnTrackingError(IBCTracking tracking, BCError value, CancellationToken cancellationToken) {
        await this._NextConsumer.OnError(value, cancellationToken);
        if (this._Tracking.TryRemove(tracking.GetId(), out _)) {
            if (BCLifeTime.Completing == this._LifeTime) {
                if (this._Tracking.IsEmpty) {
                    if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                        this._Completion.SetResult();
                        await this._NextConsumer.OnComplete(cancellationToken);
                    }
                }
            }
        }
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitSelfCompletedAsync")) {
            await this._Completion.Task.ConfigureAwait(false);
        }
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            await this._NextConsumer.WaitSelfCompletedAsync(cancellationToken);

            await this._NextConsumer.WaitRightCompletedAsync(cancellationToken);

        }
    }

    public override bool SetMonitor(BCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this._NextConsumer);
        }
        return result;
    }
}
