#pragma warning disable IDE1006 // Naming Styles

using System.Numerics;

namespace Brimborium.Channels;

public abstract class BCProcessorUnsync<TIn, TOut>
    : BCPartMonitored
    , IBCConsumer<TIn> {
    protected readonly IBCConsumer<TOut> NextConsumer;

    public BCProcessorUnsync(
            BCDescription description,
            IBCConsumer<TOut> next
        ) : base(
            description
        ) {
        this.NextConsumer = next;
    }

    public abstract Task OnNext(TIn value, CancellationToken cancellationToken);

    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        await this.NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
            BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
            await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        await this.NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    public override bool SetMonitor(BCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.NextConsumer);
        }
        return true;
    }
}


public abstract class BCProcessorUnsyncO2<TIn, TOut1, TOut2>
    : BCPartMonitored
    , IBCConsumer<TIn> {
    protected readonly IBCConsumer<TOut1> NextConsumer1;
    protected readonly IBCConsumer<TOut2> NextConsumer2;

    public BCProcessorUnsyncO2(
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
        await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
        await this.NextConsumer2.OnError(value, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
            BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
            await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.OnComplete(cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        await this.NextConsumer1.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        await this.NextConsumer2.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
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
