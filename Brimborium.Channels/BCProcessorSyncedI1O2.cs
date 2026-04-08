#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Abstract base class for dual-output processors that serialise all incoming signals
/// through a <see cref="SemaphoreSlim"/> before forwarding to two downstream consumers.
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut1">The type of values forwarded to the first downstream consumer.</typeparam>
/// <typeparam name="TOut2">The type of values forwarded to the second downstream consumer.</typeparam>
public abstract class BCProcessorSyncedI1O2<TIn, TOut1, TOut2>
    : BCPartMonitored
    , IBCConsumer<TIn> {
    protected readonly IBCConsumer<TOut1> NextConsumer1;
    protected readonly IBCConsumer<TOut2> NextConsumer2;
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCProcessorSyncedI1O2(
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
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
                await this.NextConsumer2.OnError(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            this.SetCompleting();
            if (this.SetCompleted()) {
                await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
                await this.NextConsumer2.OnComplete(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitSelfCompletedAsync))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            this._Semaphore.Release();
        }
    }
    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitRightCompletedAsync))) {
            await this.NextConsumer1.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);

            await this.NextConsumer1.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
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
        node.AddOutgoing("Out1", this.NextConsumer1);
        node.AddOutgoing("Out2", this.NextConsumer2);
    }
}
