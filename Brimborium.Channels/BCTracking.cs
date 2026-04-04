#pragma warning disable IDE1006 // Naming Styles

using System.Net.Sockets;

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

public interface IBCTrackingConsumer<TBCTracking, TOut>
    : IBCMonitored
    where TBCTracking : IBCTracking {
    Task OnNext(TBCTracking tracking, TOut value, CancellationToken cancellationToken);
    Task OnError(TBCTracking tracking, BCError error, CancellationToken cancellationToken);
    Task OnComplete(TBCTracking tracking, CancellationToken cancellationToken);
    //not so easy - is this needed?
    //Task WaitSelfCompletedAsync(TBCTracking tracking, CancellationToken cancellationToken);
    //Task WaitRightCompletedAsync(TBCTracking tracking, CancellationToken cancellationToken);
}

//public readonly record struct BCTracking(long Id) : IBCTracking {
//    public readonly long GetId() => this.Id;
//}

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
    protected readonly IBCTrackingConsumer<BCTracking<TIn, TOut>, TOut> NextTrackingConsumer;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="trackingManager"></param>
    /// <param name="nextTrackingConsumer"></param>
    public BCTracking(
            BCDescription description,
            TIn Value,
            IBCTrackingConsumer<BCTracking<TIn,TOut>, TOut> nextTrackingConsumer
        ) : base(
            description
        ) {
        this.Value = Value;
        this.NextTrackingConsumer = nextTrackingConsumer;
        this.Id = System.Threading.Interlocked.Increment(ref _NextId);
    }

    public long GetId() => this.Id;

    /// <summary>
    /// TODO
    /// </summary>
    public TIn Value { get; }

    public async Task OnNext(TOut value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this.NextTrackingConsumer.OnNext(
                this,
                value,
                cancellationToken);
        }
    }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
                BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
                await this.NextTrackingConsumer.OnComplete(
                    this,
                    cancellationToken);
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this.NextTrackingConsumer.OnError(
                this,
                value,
                cancellationToken);
        }
    }

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        // guess this will never be called
        return Task.CompletedTask;
    }

    public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        // guess this will never be called
        return Task.CompletedTask;
    }


    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.NextTrackingConsumer);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this.NextTrackingConsumer);
    }
}
