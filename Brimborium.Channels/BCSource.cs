#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// The entry point of a pipeline that pushes values into a fixed downstream consumer.
/// Acts as both an <see cref="IBCProducer"/> and an <see cref="IBCConsumer{T}"/>,
/// serialising all calls through a semaphore before forwarding them to the next consumer.
/// </summary>
/// <typeparam name="T">The type of values produced by this source.</typeparam>
public sealed class BCSource<T>
    : BCPartMonitored
    , IBCProducer
    , IBCConsumer<T> {
    private readonly IBCConsumer<T> _NextConsumer;
    private readonly SemaphoreSlim _Semaphore = new(1, 1);

    public BCSource(
            BCDescription description,
            IBCConsumer<T> nextConsumer
        ) : base(
            description
        ) {
        this._NextConsumer = nextConsumer;
    }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
                BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
                if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                    await this._NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
            await this._NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnNext(T value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
            await this._NextConsumer.OnNext(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        this._Semaphore.Release();
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
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
}

/// <summary>
/// Extension methods for <see cref="IBCConsumer{T}"/> that add convenience overloads
/// for pushing sequences of values into a consumer.
/// </summary>
public static class IBCConsumerExtension {
    extension<T>(IBCConsumer<T> that) {
        public async Task OnNextEnumerable(IEnumerable<T> listValue, CancellationToken cancellationToken) {
            foreach (var value in listValue) {
                await that.OnNext(value, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task OnNextAsyncEnumerable(IAsyncEnumerable<T> listValue, CancellationToken cancellationToken) {
            await foreach (var value in listValue.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                await that.OnNext(value, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}