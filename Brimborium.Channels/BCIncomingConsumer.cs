#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// The typed input port of a <see cref="BCBlock"/>.
/// Accepts connections from upstream producers via <see cref="IBCConsumerSubscribable{T}.OnSubscribe"/>,
/// serialises all incoming signals through a semaphore, and forwards them to the inner consumer.
/// Signals the owning block to transition its lifetime when all incoming connections have completed.
/// </summary>
/// <typeparam name="T">The type of values received on this input port.</typeparam>
public sealed class BCIncomingConsumer<T>
    : BCPartMonitored
    , IBCConsumerSubscribable<T> {
    private readonly SemaphoreSlim _Semaphore = new(1, 1);
    private IBCConnection<T>[] _ListIncomingConnection = [];
    private readonly IBCConsumer<T> _NextConsumer;
    private readonly BCBlock _Owner;

    public BCIncomingConsumer(
            BCDescription description,
            IBCConsumer<T> nextConsumer,
            BCBlock owner
        ) : base(
            description
        ) {
        this._NextConsumer = nextConsumer;
        this._Owner = owner;
    }

    public async Task OnSubscribe(IBCConnection<T> connection, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnSubscribe))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                var oldValue = this._ListIncomingConnection;
                while (true) {
                    if (
                        ReferenceEquals(
                            oldValue,
                            System.Threading.Interlocked.CompareExchange(
                                ref this._ListIncomingConnection,
                                 [.. oldValue, connection],
                                 oldValue
                            )
                            )
                    ) {
                        return;
                    } else {
                        continue;
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnNext(T value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this._NextConsumer.OnNext(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this._NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    private bool IsListIncomingConnectionCompleted() {
        foreach (var incoming in this._ListIncomingConnection) {
            if (incoming.LifeTime is BCLifeTime.Completed) {
                continue;
            } else {
                return false;
            }
        }
        return true;
    }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                this._Owner.SetCompleting();
                if (this.IsListIncomingConnectionCompleted()) {
                    BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
                    if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                        this._Owner.SetCompleted();
                        await this._NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitRightCompletedAsync))) {
            await this._NextConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this._NextConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this._NextConsumer);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this._NextConsumer);
    }
}
