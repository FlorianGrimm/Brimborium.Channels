#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public sealed class BCSource<T>
    : IBCProducer
    , IBCConsumer<T>
    , IBCMonitored {
    private readonly IBCConsumer<T> _NextConsumer;
    private BCLifeTime _LifeTime;
    private BCMonitor? _Monitor;

    public BCDescription Description { get; set; }

    public BCSource(
        BCDescription? description,
        IBCConsumer<T> nextConsumer
        ) {
        this.Description = description ?? new();
        this._NextConsumer = nextConsumer;
    }

    public BCLifeTime LifeTime => this._LifeTime;

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
            if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                await this._NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            await this._NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task OnNext(T value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            await this._NextConsumer.OnNext(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        await this._NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        monitor.Add(this._NextConsumer);
        return true;
    }
}

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