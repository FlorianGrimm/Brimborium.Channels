#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public sealed class BCIncomingConsumer<T>
    : IBCConsumerSubscribable<T>
    , IBCMonitored {
    private BCLifeTime _LifeTime;
    private readonly SemaphoreSlim _Semaphore = new(1, 1);
    private IBCConnection<T>[] _ListIncomingConnection = [];
    private BCMonitor? _Monitor;
    private readonly IBCConsumer<T> _Consumer;
    private readonly BCBlock _Owner;

    public BCLifeTime LifeTime => this._LifeTime;

    public BCDescription Description { get; set; }

    public BCIncomingConsumer(
        BCDescription? description,
        IBCConsumer<T> consumer,
        BCBlock owner) {
        this.Description = description ?? new();
        this._Consumer = consumer;
        this._Owner = owner;
    }

    public async Task OnSubscribe(IBCConnection<T> connection, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnSubscribe")) {
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
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this._Consumer.OnNext(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this._Consumer.OnError(value, cancellationToken).ConfigureAwait(false);
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
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                this._Owner.SetCompleting();
                if (this.IsListIncomingConnectionCompleted()) {
                    BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
                    if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                        this._Owner.SetCompleted();
                        await this._Consumer.OnComplete(cancellationToken).ConfigureAwait(false);
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            await this._Consumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public void SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return; }
        this._Monitor = monitor;
        monitor.Add(this._Consumer);
    }
}
