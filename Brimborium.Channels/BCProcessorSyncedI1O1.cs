#pragma warning disable IDE1006 // Naming Styles

using System.Runtime.CompilerServices;

namespace Brimborium.Channels;

/// <summary>
/// Abstract base class for single-output processors that serialise all incoming signals
/// (OnNext, OnError, OnComplete) through a <see cref="SemaphoreSlim"/> before forwarding to the next consumer.
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut">The type of values forwarded to the downstream consumer.</typeparam>
public abstract class BCProcessorSyncedI1O1<TIn, TOut>
    : BCPartMonitored
    , IBCConsumer<TIn> {
    protected readonly IBCConsumer<TOut> NextConsumer;
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCProcessorSyncedI1O1(
            BCDescription description,
            IBCConsumer<TOut> nextConsumer
        ) : base(
            description
        ) {
        this.NextConsumer = nextConsumer;
    }

    // public Task OnSubscripe(IBCConnection<TIn> connection, CancellationToken cancellationToken) {
    //     throw new NotSupportedException();
    // }

    public abstract Task OnNext(TIn value, CancellationToken cancellationToken);

    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this.NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            this.SetCompleting();
            if (this.SetCompleted()) {
                await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
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
            await this.NextConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        monitor.Add(this.NextConsumer);
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing("Next", this.NextConsumer);
    }
}
