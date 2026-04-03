#pragma warning disable IDE1006 // Naming Styles

using System.Runtime.CompilerServices;

namespace Brimborium.Channels;

/// <summary>
/// Abstract base class for single-output processors that serialise all incoming signals
/// (OnNext, OnError, OnComplete) through a <see cref="SemaphoreSlim"/> before forwarding to the next consumer.
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut">The type of values forwarded to the downstream consumer.</typeparam>
public abstract class BCProcessorSynced<TIn, TOut>
    : BCPartMonitored
    , IBCConsumer<TIn> {
    protected readonly IBCConsumer<TOut> NextConsumer;
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCProcessorSynced(
            BCDescription description,
            IBCConsumer<TOut> next
        ) : base(
            description
        ) {
        this.NextConsumer = next;
    }

    // public Task OnSubscripe(IBCConnection<TIn> connection, CancellationToken cancellationToken) {
    //     throw new NotSupportedException();
    // }

    public abstract Task OnNext(TIn value, CancellationToken cancellationToken);

    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        try {
            await this.NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
        } finally {
            this._Semaphore.Release();
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
            BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
            await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        this._Semaphore.Release();
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            await this.NextConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        monitor.Add(this.NextConsumer);
        return true;
    }
}



/// <summary>
/// Abstract base class for dual-output processors that serialise all incoming signals
/// through a <see cref="SemaphoreSlim"/> before forwarding to two downstream consumers.
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut1">The type of values forwarded to the first downstream consumer.</typeparam>
/// <typeparam name="TOut2">The type of values forwarded to the second downstream consumer.</typeparam>
public abstract class BCProcessorSyncedO2<TIn, TOut1, TOut2>
    : BCPartMonitored
    , IBCConsumer<TIn> {
    protected readonly IBCConsumer<TOut1> NextConsumer1;
    protected readonly IBCConsumer<TOut2> NextConsumer2;
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCProcessorSyncedO2(
            BCDescription description,
            IBCConsumer<TOut1> nextConsumer1,
            IBCConsumer<TOut2> nextConsumer2
        ) : base(
            description
        ) {
        this.NextConsumer1 = nextConsumer1;
        this.NextConsumer2 = nextConsumer2;
    }

    public Task OnSubscripe(IBCConnection<TIn> connection, CancellationToken cancellationToken) {
        throw new NotSupportedException();
    }

    public abstract Task OnNext(TIn value, CancellationToken cancellationToken);

    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        try {
            await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.OnError(value, cancellationToken).ConfigureAwait(false);
        } finally {
            this._Semaphore.Release();
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
            BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
            await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.OnComplete(cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitSelfCompletedAsync")) {
            await this._Semaphore.WaitAsync(cancellationToken);
            this._Semaphore.Release();
        }
    }
    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            await this.NextConsumer1.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);

            await this.NextConsumer1.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(BCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.NextConsumer1);
            monitor.Add(this.NextConsumer2);
        }
        return true;
    }
}
