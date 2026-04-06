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
/// <typeparam name="TOut1">The type of the output value produced by this tracking unit.</typeparam>
public class BCTracking<TIn, TOut1>
    : BCTracking
    , IBCTrackingIn<TIn> {
    private readonly SemaphoreSlim _Semaphore = new(1, 1);
    private readonly IBCTrackingManager _TrackingManager;
    protected readonly IBCConsumer<BCMessage<TIn, TOut1>> NextConsumer1;

    public BCTracking(
            BCDescription description,
            TIn Value,
            IBCTrackingManager trackingManager,
            IBCConsumer<BCMessage<TIn, TOut1>> nextConsumer1
        ) : base(
            description
        ) {
        this.Value = Value;
        this._TrackingManager = trackingManager;
        this.NextConsumer1 = nextConsumer1;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public TIn Value { get; }

    public async Task OnNext1(TOut1 value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this.NextConsumer1.OnNext(
                    BCMessage<TIn, TOut1>.OnNext(this.Value, value),
                    cancellationToken);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                this.SetCompleting();
                try {
                    await this.NextConsumer1.OnNext(
                        BCMessage<TIn, TOut1>.OnComplete(this.Value),
                        cancellationToken);

                } finally {
                    if (this._TrackingManager.RemoveTracking(this)) {
                        if (this.SetCompleted()) {
                            await this.NextConsumer1.OnComplete(cancellationToken);
                        }
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override async Task OnError(BCError error, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            try {
                await this.NextConsumer1.OnNext(
                    BCMessage<TIn, TOut1>.OnError(this.Value, error),
                    cancellationToken);
            } finally {
                this._Semaphore.Release();
            }
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
            monitor.Add(this.NextConsumer1);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this.NextConsumer1);
    }
}


/// <summary>
/// Represents a single in-flight request created by a tracking processor.
/// Holds the original input <see cref="Value"/> and forwards output signals to the downstream consumer.
/// Reports its own completion or failure back to the <see cref="IBCTrackingManager"/> so the manager
/// can decide when all work is done and the downstream <c>OnComplete</c> can be forwarded.
/// </summary>
/// <typeparam name="TIn">The type of the original input value.</typeparam>
/// <typeparam name="TOut1">The type of the output value produced by this tracking unit.</typeparam>
/// <typeparam name="TOut2">The type of the output value produced by this tracking unit.</typeparam>
public class BCTracking<TIn, TOut1, TOut2>
    : BCTracking
    , IBCTrackingIn<TIn> {
    private readonly SemaphoreSlim _Semaphore = new(1, 1);
    private readonly IBCTrackingManager _TrackingManager;
    private readonly IBCConsumer<BCMessage<TIn, TOut1>> NextConsumer1;
    private readonly IBCConsumer<BCMessage<TIn, TOut2>> NextConsumer2;

    public BCTracking(
            BCDescription description,
            TIn Value,
            IBCTrackingManager trackingManager,
            IBCConsumer<BCMessage<TIn, TOut1>> nextConsumer1,
            IBCConsumer<BCMessage<TIn, TOut2>> nextConsumer2
        ) : base(
            description
        ) {
        this.Value = Value;
        this._TrackingManager = trackingManager;
        this.NextConsumer1 = nextConsumer1;
        this.NextConsumer2 = nextConsumer2;
    }

    public TIn Value { get; }

    public async Task OnNext1(TOut1 value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this.NextConsumer1.OnNext(
                    BCMessage<TIn, TOut1>.OnNext(this.Value, value),
                    cancellationToken);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnNext2(TOut2 value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this.NextConsumer2.OnNext(
                    BCMessage<TIn, TOut2>.OnNext(this.Value, value),
                    cancellationToken);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                this.SetCompleting();

                try {
                    await this.NextConsumer1.OnNext(
                        BCMessage<TIn, TOut1>.OnComplete(this.Value),
                        cancellationToken);
                    await this.NextConsumer2.OnNext(
                        BCMessage<TIn, TOut2>.OnComplete(this.Value),
                        cancellationToken);
                } finally {
                    if (this._TrackingManager.RemoveTracking(this)) {
                        if (this.SetCompleted()) {
                            await this.NextConsumer1.OnComplete(cancellationToken);
                        }
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override async Task OnError(BCError error, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            try {
                await this.NextConsumer1.OnNext(
                    BCMessage<TIn, TOut1>.OnError(this.Value, error),
                    cancellationToken);
                await this.NextConsumer2.OnNext(
                    BCMessage<TIn, TOut2>.OnError(this.Value, error),
                    cancellationToken);
            } finally {
                this._Semaphore.Release();
            }
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
            monitor.Add(this.NextConsumer1);
            monitor.Add(this.NextConsumer2);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this.NextConsumer1);
        node.AddOutgoing(this.NextConsumer2);
    }
}
