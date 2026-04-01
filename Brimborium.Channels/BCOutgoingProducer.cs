#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public sealed class BCOutgoingProducer<T> 
    : IBCConsumer<T>, IBCProducer<T>
    , IBCMonitored {
    private BCLifeTime _LifeTime;
    private readonly SemaphoreSlim _Semaphore = new(1, 1);
    private IBCConnection<T>[] _ListOutgoingConnection = [];
    private BCMonitor? _Monitor;

    public BCLifeTime LifeTime => this._LifeTime;
    public BCDescription Description { get; set; }

    public BCOutgoingProducer(
        BCDescription? description
    ) {
        this.Description = description ?? new();
    }

    public async Task<IBCConnection<T>> Subscribe(IBCConsumerSubscribable<T> next, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "Subscribe")) {
            var connection = new BCConnection<T>(this, next);
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                var oldValue = this._ListOutgoingConnection;
                while (true) {
                    if (
                        ReferenceEquals(
                            oldValue,
                            System.Threading.Interlocked.CompareExchange(
                                ref this._ListOutgoingConnection,
                                 [.. oldValue, connection],
                                 oldValue
                            )
                            )
                    ) {
                        break;
                    } else {
                        continue;
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
            if (this._Monitor is { } monitor) { monitor.Add(connection).Add(next); }
            await next.OnSubscribe(connection, cancellationToken).ConfigureAwait(false);
            return connection;
        }
    }

    public async Task OnNext(T value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                foreach (var consumer in this._ListOutgoingConnection) {
                    if (BCLifeTime.Active == consumer.LifeTime) {
                        await consumer.OnNext(value, cancellationToken).ConfigureAwait(false);
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                foreach (var consumer in this._ListOutgoingConnection) {
                    if (BCLifeTime.Active == consumer.LifeTime) {
                        await consumer.OnError(value, cancellationToken).ConfigureAwait(false);
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
            if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    foreach (var consumer in this._ListOutgoingConnection) {
                        await consumer.OnComplete(cancellationToken).ConfigureAwait(false);
                    }
                } finally {
                    this._Semaphore.Release();
                }
            }
        }
    }

    public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            foreach (var connection in this._ListOutgoingConnection) {
                await connection.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public void SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return; }
        this._Monitor = monitor;
        foreach (var consumer in this._ListOutgoingConnection) {
            monitor.Add(consumer);
        }
    }
}
