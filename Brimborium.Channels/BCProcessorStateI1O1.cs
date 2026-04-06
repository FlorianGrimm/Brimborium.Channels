#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public class BCProcessorStateI1O1<TState, TIn, TOut>
    : BCProcessorSyncedI1O1<TIn, TOut> {
    private readonly TState _State;
    private readonly Func<TIn, TState, IBCConsumer<TOut>, CancellationToken, Task> _OnNext;
    private readonly Func<BCError, TState, IBCConsumer<TOut>, CancellationToken, Task>? _OnError;
    private readonly Func<TState, IBCConsumer<TOut>, CancellationToken, Task>? _OnComplete;

    public BCProcessorStateI1O1(
            BCDescription description,
            TState state,
            Func<TIn, TState, IBCConsumer<TOut>, CancellationToken, Task> onNext,
            Func<BCError, TState, IBCConsumer<TOut>, CancellationToken, Task>? onError,
            Func<TState, IBCConsumer<TOut>, CancellationToken, Task>? onComplete,
            IBCConsumer<TOut> nextConsumer
        ) : base(
            description, nextConsumer
        ) {
        this._State = state;
        this._OnNext = onNext;
        this._OnError = onError;
        this._OnComplete = onComplete;
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try {
            await this._OnNext(value, this._State, this.NextConsumer, cancellationToken).ConfigureAwait(false);
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (this._OnError is { } onError) {
                    await onError(value, this._State, this.NextConsumer, cancellationToken).ConfigureAwait(false);
                } else { 
                    await this.NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
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
                    await onComplete(this._State, this.NextConsumer, cancellationToken).ConfigureAwait(false);
                } else { 
                    await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
