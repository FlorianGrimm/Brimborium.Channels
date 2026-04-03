#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// A processor that applies caller-supplied delegates for <c>OnNext</c>,
/// and optionally <c>OnError</c> and <c>OnComplete</c>.
/// All calls are serialised through a semaphore (inherits <see cref="BCProcessorSynced{TIn,TOut}"/>).
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut">The type of values forwarded to the downstream consumer.</typeparam>
public sealed class BCDelegate<TIn, TOut>
    : BCProcessorSynced<TIn, TOut>
    , IBCMonitored {
    private readonly Func<TIn, IBCConsumer<TOut>, CancellationToken, Task> _onNext;
    private readonly Func<BCError, IBCConsumer<TOut>, CancellationToken, Task>? _OnError;
    private readonly Func<IBCConsumer<TOut>, CancellationToken, Task>? _OnComplete;

    public BCDelegate(
            BCDescription description,
            Func<TIn, IBCConsumer<TOut>, CancellationToken, Task> onNext,
            Func<BCError, IBCConsumer<TOut>, CancellationToken, Task>? onError,
            Func<IBCConsumer<TOut>, CancellationToken, Task>? onComplete,
            IBCConsumer<TOut> next
        ) : base(
            description,
            next
        ) {
        this._onNext = onNext;
        this._OnError = onError;
        this._OnComplete = onComplete;
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        try {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
                await this._onNext(value, this.NextConsumer, cancellationToken).ConfigureAwait(false);
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
        using (this._Monitor?.LogEnter(this, "OnError")) {
            try {
                if (this._OnError is { } onError) {
                    await this._Semaphore.WaitAsync(cancellationToken);
                    try {
                        await onError(value, this.NextConsumer, cancellationToken).ConfigureAwait(false);
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
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
                if (this.SetCompleting()) {
                    this.SetCompleted();
                    if (this._OnComplete is { } onComplete) {
                        await onComplete(this.NextConsumer, cancellationToken);
                    }
                    await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }
}