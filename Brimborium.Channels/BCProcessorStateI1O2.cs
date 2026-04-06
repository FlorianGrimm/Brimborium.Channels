#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public class BCProcessorStateI1O2<TState, TIn, TOut1, TOut2>
    : BCProcessorSyncedI1O2<TIn, TOut1, TOut2> {
    private readonly TState _State;
    private readonly Func<TIn, TState, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task> _OnNext;
    private readonly Func<BCError, TState, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? _OnError;
    private readonly Func<TState, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? _OnComplete;

    public BCProcessorStateI1O2(
            BCDescription description,
            TState state,
            Func<TIn, TState, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task> onNext,
            Func<BCError, TState, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? onError,
            Func<TState, IBCConsumer<TOut1>, IBCConsumer<TOut2>, CancellationToken, Task>? onComplete,
            IBCConsumer<TOut1> nextConsumer1,
            IBCConsumer<TOut2> nextConsumer2
        ) : base(
            description, nextConsumer1, nextConsumer2
        ) {
        this._State = state;
        this._OnNext = onNext;
        this._OnError = onError;
        this._OnComplete = onComplete;
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try {
            await this._OnNext(value, this._State, this.NextConsumer1, this.NextConsumer2, cancellationToken).ConfigureAwait(false);
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (this._OnError is { } onError) {
                    await onError(value, this._State, this.NextConsumer1, this.NextConsumer2, cancellationToken).ConfigureAwait(false);
                } else {
                    await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
                    await this.NextConsumer2.OnError(value, cancellationToken).ConfigureAwait(false);
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            this.SetCompleting();
            if (this.SetCompleted()) {
                if (this._OnComplete is { } onComplete) {
                    await onComplete(this._State, this.NextConsumer1, this.NextConsumer2, cancellationToken).ConfigureAwait(false);
                } else {
                    await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
                    await this.NextConsumer2.OnComplete(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}

