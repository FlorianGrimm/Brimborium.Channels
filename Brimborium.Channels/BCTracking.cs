#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>Non-generic marker interface for an in-flight tracking unit; exposes its unique id.</summary>
public interface IBCTracking {
    long GetId();
}

/// <summary>
/// Typed tracking interface that combines consumer signals, monitoring support,
/// and the non-generic <see cref="IBCTracking"/> id marker.
/// </summary>
/// <typeparam name="TOut">The type of output values this tracking unit can receive.</typeparam>
public interface IBCTracking<TOut>
    : IBCConsumer<TOut>
    , IBCMonitored
    , IBCTracking {
}


/// <summary>
/// Represents a single in-flight request created by a tracking processor.
/// Holds the original input <see cref="Value"/> and forwards output signals to the downstream consumer.
/// Reports its own completion or failure back to the <see cref="IBCTrackingManager"/> so the manager
/// can decide when all work is done and the downstream <c>OnComplete</c> can be forwarded.
/// </summary>
/// <typeparam name="TIn">The type of the original input value.</typeparam>
/// <typeparam name="TOut">The type of the output value produced by this tracking unit.</typeparam>
public class BCTracking<TIn, TOut>
    : BCPartMonitored
    , IBCConsumer<TOut>
    , IBCTracking
    , IBCTracking<TOut> {
    private static long _NextId;
    internal readonly long Id;
    protected readonly IBCTrackingManager _TrackingManager;
    private readonly IBCConsumer<TOut> _NextConsumer;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="trackingManager"></param>
    /// <param name="nextConsumer"></param>
    public BCTracking(
            BCDescription description,
            TIn Value,
            IBCTrackingManager trackingManager,
            IBCConsumer<TOut> nextConsumer
        ) : base(
            description
        ) {
        this.Value = Value;
        this._TrackingManager = trackingManager;
        this._NextConsumer = nextConsumer;
        this.Id = System.Threading.Interlocked.Increment(ref _NextId);
    }

    public long GetId() => this.Id;

    /// <summary>
    /// TODO
    /// </summary>
    public TIn Value { get; }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
                BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
                await this._TrackingManager.OnTrackingComplete(this, cancellationToken);
                if (this._Completion is { } completion) {
                    completion.SetResult();
                }
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            await this._TrackingManager.OnTrackingError(this, value, cancellationToken);
        }
    }

    private TaskCompletionSource? _Completion;

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitSelfCompletedAsync")) {
            if (this._Completion is { } completion) {
                await completion.Task;
            }
            if (BCLifeTime.Completed == this._LifeTime) {
                return;
            }
            this._Completion = new TaskCompletionSource();
            await this._Completion.Task;
        }
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            await this._NextConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this._NextConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task OnNext(TOut value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            await this._NextConsumer.OnNext(value, cancellationToken);
        }
    }

    public override bool SetMonitor(BCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this._NextConsumer);
        }
        return true;
    }
}
