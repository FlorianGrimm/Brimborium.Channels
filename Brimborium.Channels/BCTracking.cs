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
public interface IBCTrackingOut<TOut>
    : IBCConsumer<TOut>
    , IBCMonitored
    , IBCTracking {
}
public interface IBCTrackingIn<TIn>
    : IBCTracking {
    TIn Value { get; }
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

public interface IBCTracking<TIn, TOut>
    : IBCTrackingOut<TOut>
    , IBCTrackingIn<TIn> {
}


//public readonly record struct BCTracking(long Id) : IBCTracking {
//    public readonly long GetId() => this.Id;
//}

public abstract class BCTracking
    : BCPartMonitored
    , IBCConsumer
    , IBCTracking {
    private static long _NextId;
    internal readonly long Id;

    protected BCTracking(
            BCDescription description
        ) : base(
            description
        ) {
        this.Id = System.Threading.Interlocked.Increment(ref _NextId);
    }

    public long GetId() => this.Id;

    public virtual Task OnError(BCError value, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public virtual Task OnComplete(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

}

/// <summary>
/// Represents a single in-flight request created by a tracking processor.
/// Holds the original input <see cref="Value"/> and forwards output signals to the downstream consumer.
/// Reports its own completion or failure back to the <see cref="IBCTrackingManager"/> so the manager
/// can decide when all work is done and the downstream <c>OnComplete</c> can be forwarded.
/// </summary>
/// <typeparam name="TIn">The type of the original input value.</typeparam>
/// <typeparam name="TOut">The type of the output value produced by this tracking unit.</typeparam>
public abstract class BCTracking<TIn, TOut, TBCTracking>
    : BCTracking
    , IBCConsumer<TOut>
    , IBCTrackingOut<TOut>
    , IBCTrackingIn<TIn>
    where TBCTracking : IBCTracking {
    protected readonly IBCTrackingConsumer<TBCTracking, TOut> NextTrackingConsumer;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="trackingManager"></param>
    /// <param name="nextTrackingConsumer"></param>
    public BCTracking(
            BCDescription description,
            TIn Value,
            IBCTrackingConsumer<TBCTracking, TOut> nextTrackingConsumer
        ) : base(
            description
        ) {
        this.Value = Value;
        this.NextTrackingConsumer = nextTrackingConsumer;
    }

    protected abstract TBCTracking GetNextTracking();

    /// <summary>
    /// TODO
    /// </summary>
    public TIn Value { get; }

    public async Task OnNext(TOut value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this.NextTrackingConsumer.OnNext(
                this.GetNextTracking(),
                value,
                cancellationToken);
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            this.SetCompleting();
            if (this.SetCompleted()) {
                await this.NextTrackingConsumer.OnComplete(
                    this.GetNextTracking(),
                    cancellationToken);
            }
        }
    }

    public override async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this.NextTrackingConsumer.OnError(
                this.GetNextTracking(),
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

public class BCTracking<TIn, TOut>
    : BCTracking<TIn, TOut, BCTracking<TIn, TOut>> {
    public BCTracking(
            BCDescription description, TIn Value, IBCTrackingConsumer<BCTracking<TIn, TOut>, TOut> nextTrackingConsumer
        ) : base(
            description, Value, nextTrackingConsumer
        ) {
    }

    protected override BCTracking<TIn, TOut> GetNextTracking() => this;
}