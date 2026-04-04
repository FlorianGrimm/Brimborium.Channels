#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// The typed output port of a <see cref="BCBlock"/> that implements <see cref="IBCProducer{T}"/>.
/// Maintains a list of active outgoing <see cref="IBCConnection{T}"/> subscriptions and fans out
/// every <c>OnNext</c>, <c>OnError</c>, and <c>OnComplete</c> signal to all active connections.
/// </summary>
/// <typeparam name="T">The type of values emitted by this producer.</typeparam>
public sealed class BCOutgoingProducer<T>
    : BCPartMonitored
    , IBCConsumer<T>, IBCProducer<T> {
    private readonly SemaphoreSlim _Semaphore = new(1, 1);
    private IBCConnection<T>[] _ListOutgoingConnection = [];
    
    public BCOutgoingProducer(
            BCDescription description
        ) : base(
            description
        ) {
    }

    public async Task<IBCConnection<T>> Subscribe(IBCConsumerSubscribable<T> next, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "Subscribe")) {
            var connection = new BCConnection<T>(this.Description, this, next);
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

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            foreach (var connection in this._ListOutgoingConnection) {
                await connection.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
                await connection.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            foreach (var consumer in this._ListOutgoingConnection) {
                monitor.Add(consumer);
            }
        }
        return true;
    }
}
