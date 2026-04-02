#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public abstract class BCProcessorSynced<TIn, TOut>
    : IBCConsumer<TIn>
    , IBCMonitored {
    private BCLifeTime _LifeTime;
    private BCMonitor? _Monitor;
    protected readonly IBCConsumer<TOut> NextConsumer;
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCLifeTime LifeTime => this._LifeTime;
    public BCDescription Description { get; set; }

    public BCProcessorSynced(
        BCDescription? description,
        IBCConsumer<TOut> next
    ) {
        this.Description = description ?? new();
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

    public virtual async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        this._Semaphore.Release();
        await this.NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    protected bool SetCompleting() {
        return BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
    }

    protected bool SetCompleted() {
        return BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public virtual bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        monitor.Add(this.NextConsumer);
        return true;
    }
}



public abstract class BCProcessorSyncedO2<TIn, TOut1, TOut2>
    : IBCConsumer<TIn>
    , IBCMonitored{
    private BCLifeTime _LifeTime;
    protected BCMonitor? _Monitor;
    protected readonly IBCConsumer<TOut1> NextConsumer1;
    protected readonly IBCConsumer<TOut2> NextConsumer2;
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCLifeTime LifeTime => this._LifeTime;
    public BCDescription Description { get; set; }

    public BCProcessorSyncedO2(
        BCDescription? description,
        IBCConsumer<TOut1> nextConsumer1,
        IBCConsumer<TOut2> nextConsumer2
    ) {
        this.Description = description ?? new();
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

    public virtual async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        this._Semaphore.Release();
        await this.NextConsumer1.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        await this.NextConsumer2.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    protected bool SetCompleting() {
        return BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
    }

    protected bool SetCompleted() {
        return BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public virtual bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        monitor.Add(this.NextConsumer1);
        monitor.Add(this.NextConsumer2);
        return true;
    }
}
