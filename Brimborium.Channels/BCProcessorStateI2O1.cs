#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public class BCProcessorStateI2O1<TState, TIn1, TIn2, TOut1>
    : BCProcessorSyncedI2O1<TIn1, TIn2, TOut1> {
    private readonly TState _State;
    private readonly Func<TIn1, TState, IBCConsumer<TOut1>, CancellationToken, Task> _OnNext1;
    private readonly Func<TIn2, TState, IBCConsumer<TOut1>, CancellationToken, Task> _OnNext2;
    private readonly Func<BCError, TState, IBCConsumer<TOut1>, CancellationToken, Task>? _OnError;
    private readonly Func<bool, TState, IBCConsumer<TOut1>, CancellationToken, Task>? _OnComplete1;
    private readonly Func<bool, TState, IBCConsumer<TOut1>, CancellationToken, Task>? _OnComplete2;

    public BCProcessorStateI2O1(
            BCDescription description,
            TState state,
            Func<TIn1, TState, IBCConsumer<TOut1>, CancellationToken, Task> onNext1,
            Func<TIn2, TState, IBCConsumer<TOut1>, CancellationToken, Task> onNext2,
            Func<BCError, TState, IBCConsumer<TOut1>, CancellationToken, Task>? onError,
            Func<bool, TState, IBCConsumer<TOut1>, CancellationToken, Task>? onComplete1,
            Func<bool, TState, IBCConsumer<TOut1>, CancellationToken, Task>? onComplete2,
            IBCConsumer<TOut1> nextConsumer1
        ) : base(
            description, nextConsumer1
        ) {
        this._State = state;
        this._OnNext1 = onNext1;
        this._OnNext2 = onNext2;
        this._OnError = onError;
        this._OnComplete1 = onComplete1;
        this._OnComplete2 = onComplete2;
    }

    public override async Task OnNext1(TIn1 value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            await this._OnNext1(value, this._State, this.NextConsumer1, cancellationToken).ConfigureAwait(false);
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnNext2(TIn2 value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            await this._OnNext2(value, this._State, this.NextConsumer1, cancellationToken).ConfigureAwait(false);
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnError1(BCError value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (this._OnError is { } onError) {
                await this._OnError(value, this._State, this.NextConsumer1, cancellationToken).ConfigureAwait(false);
            } else {
                await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
            }
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnError2(BCError value, CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (this._OnError is { } onError) {
                await this._OnError(value, this._State, this.NextConsumer1, cancellationToken).ConfigureAwait(false);
            } else {
                await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
            }
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnComplete1(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (this._OnComplete1 is { } onComplete1) {
                var allCompleted = ((BCLifeTime.Completed == this._Consumer1.LifeTime)
                    && (BCLifeTime.Completed == this._Consumer2.LifeTime));
                await onComplete1(allCompleted, this._State, this.NextConsumer1, cancellationToken).ConfigureAwait(false);
            } else {
                await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
            }
        } finally {
            this._Semaphore.Release();
        }
    }

    public override async Task OnComplete2(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (this._OnComplete2 is { } onComplete2) {
                var allCompleted = ((BCLifeTime.Completed == this._Consumer1.LifeTime)
                    && (BCLifeTime.Completed == this._Consumer2.LifeTime));
                await onComplete2(allCompleted, this._State, this.NextConsumer1, cancellationToken).ConfigureAwait(false);
            } else {
                await this.NextConsumer1.OnComplete(cancellationToken).ConfigureAwait(false);
            }
        } finally {
            this._Semaphore.Release();
        }
    }

    
}

