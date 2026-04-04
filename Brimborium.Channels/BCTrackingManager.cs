#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

/// <summary>
/// Coordinates in-flight tracking units for a tracking processor.
/// Tracks active <see cref="IBCTracking"/> instances by id and delays the downstream
/// <c>OnComplete</c> signal until all registered trackings have finished.
/// </summary>
public interface IBCTrackingManager : IBCPart {
    /// <summary>
    /// Add the tracking
    /// </summary>
    /// <param name="tracking"></param>
    void AddTracking<TBCTracking>(TBCTracking tracking)
        where TBCTracking : IBCTracking;

    /// <summary>
    /// Remove tracking
    /// </summary>
    /// <param name="tracking"></param>
    /// <param name="cancellationToken">stop me</param>
    /// <returns>true if OnComplete must be send</returns>
    bool RemoveTracking<TBCTracking>(TBCTracking tracking)
        where TBCTracking : IBCTracking;

    /// <summary>
    /// The left has received a OnComplete
    /// </summary>
    /// <returns></returns>
    bool OnComplete();
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

    public BCTrackingManager(
            BCDescription description
        ) : base(
            description
        ) {
    }

    public void AddTracking<TBCTracking>(TBCTracking tracking)
        where TBCTracking : IBCTracking {
        var id = tracking.GetId();
        if (this._Tracking.TryAdd(id, tracking)) {
            return;
        } else {
            throw new ArgumentException("a tracking with the same id is already present");
        }
    }

    public bool RemoveTracking<TBCTracking>(TBCTracking tracking) where TBCTracking : IBCTracking {
        if (this._Tracking.TryRemove(tracking.GetId(), out _)) {
            if (BCLifeTime.Completing == this._LifeTime) {
                if (this._Tracking.IsEmpty) {
                    if (this.SetCompleted()) {
                        this._Completion.SetResult();
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool OnComplete() {
        if (this.SetCompleting()) {
            if (this._Tracking.IsEmpty) {
                if (this.SetCompleted()) {
                    this._Completion.SetResult();
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Wait for all
    /// </summary>
    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return this._Completion.Task;
    }

    /// <summary>no nextConsumer</summary>
    public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    // called from left
    //public async Task OnComplete(CancellationToken cancellationToken) {
    //    using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
    //        if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
    //            if (this._Tracking.IsEmpty) {
    //                if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
    //                    this._Completion.SetResult();
    //                    await this._NextConsumer.OnComplete(cancellationToken);
    //                }
    //            }
    //        }
    //    }
    //}

    //// called from left
    //public async Task OnError(BCError value, CancellationToken cancellationToken) {
    //    using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
    //        await this._NextConsumer.OnError(value, cancellationToken);
    //    }
    //}

    //// call from down
    //public async Task OnTrackingComplete<TBCTracking>(TBCTracking tracking, CancellationToken cancellationToken)
    //    where TBCTracking : IBCTracking {
    //    if (this._Tracking.TryRemove(tracking.GetId(), out _)) {
    //        if (BCLifeTime.Completing == this._LifeTime) {
    //            if (this._Tracking.IsEmpty) {
    //                if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
    //                    this._Completion.SetResult();
    //                    await this._NextConsumer.OnComplete(cancellationToken);
    //                }
    //            }
    //        }
    //    }
    //}

    //// call from down
    //public async Task OnTrackingError<TBCTracking>(TBCTracking tracking, BCError value, CancellationToken cancellationToken)
    //    where TBCTracking : IBCTracking {
    //    await this._NextConsumer.OnError(value, cancellationToken);
    //    if (this._Tracking.TryRemove(tracking.GetId(), out _)) {
    //        if (BCLifeTime.Completing == this._LifeTime) {
    //            if (this._Tracking.IsEmpty) {
    //                if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
    //                    this._Completion.SetResult();
    //                    await this._NextConsumer.OnComplete(cancellationToken);
    //                }
    //            }
    //        }
    //    }
    //}

    //public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
    //    using (this._Monitor?.LogEnter(this, nameof(this.WaitSelfCompletedAsync))) {
    //        await this._Completion.Task.ConfigureAwait(false);
    //    }
    //}

    //public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
    //    using (this._Monitor?.LogEnter(this, nameof(this.WaitRightCompletedAsync))) {
    //        await this._NextConsumer.WaitSelfCompletedAsync(cancellationToken);

    //        await this._NextConsumer.WaitRightCompletedAsync(cancellationToken);

    //    }
    //}

    //public override bool SetMonitor(BCMonitor monitor) {
    //    var result = base.SetMonitor(monitor);
    //    if (result) {
    //        monitor.Add(this._NextConsumer);
    //    }
    //    return result;
    //}


}
