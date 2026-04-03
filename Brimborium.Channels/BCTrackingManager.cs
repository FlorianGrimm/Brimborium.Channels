#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

/// <summary>
/// TODO
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
/// TODO
/// </summary>
public class BCTrackingManager 
    : BCPartMonitored
    , IBCTrackingManager {
    private readonly ConcurrentDictionary<long, IBCTracking> _Tracking = new();
    private readonly TaskCompletionSource _Completion=new();
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

    public override Task WaitCompletedAsync(CancellationToken cancellationToken) {
        return this._Completion.Task;
    }
}
