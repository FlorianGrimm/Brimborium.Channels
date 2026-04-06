#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public sealed class BCDelegateI1O2<TIn, TOut1, TOut2>
    : BCProcessorSyncedI1O2<TIn, TOut1, TOut2>
    , IBCMonitored {
    private readonly Func<TIn, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task> _onNext;
    private readonly Func<BCError, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? _OnError;
    private readonly Func<IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? _OnComplete;

    public BCDelegateI1O2(
            BCDescription description,
            Func<TIn, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task> onNext,
            Func<BCError, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? onError,
            Func<IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? onComplete,
            IBCConsumer<TOut1> nextConsumer1,
            IBCConsumer<TOut2> nextConsumer2
        ) : base(
            description,
            nextConsumer1,
            nextConsumer2
        ) {
        this._onNext = onNext;
        this._OnError = onError;
        this._OnComplete = onComplete;
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        try {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this._onNext(value, this.NextConsumer1, this.NextConsumer2, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        } catch (Exception error) {
            BCError bcError = new(error);
            await this.OnError(bcError, cancellationToken).ConfigureAwait(false);
            bcError.ThrowIfNotHandled();
        }
    }

    public override async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            try {
                if (this._OnError is { } onError) {
                    await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try {
                        await onError(value, this.NextConsumer1, this.NextConsumer2, cancellationToken).ConfigureAwait(false);
                    } finally {
                        this._Semaphore.Release();
                    }
                } else {
                    await base.OnError(value, cancellationToken).ConfigureAwait(false);
                }
            } catch (Exception error) {
                BCError bcError = new(error);
                await base.OnError(bcError, cancellationToken).ConfigureAwait(false);
                bcError.ThrowIfNotHandled();
            }
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (this.SetCompleting()) {
                    this.SetCompleted();
                    if (this._OnComplete is { } onComplete) {
                        await onComplete(this.NextConsumer1, this.NextConsumer2, cancellationToken);
                    }
                    await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
                    await this.NextConsumer2.OnComplete(cancellationToken).ConfigureAwait(false);
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }
}