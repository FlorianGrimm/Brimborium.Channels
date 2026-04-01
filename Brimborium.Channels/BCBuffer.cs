#pragma warning disable IDE1006 // Naming Styles

using System.Threading.Channels;

namespace Brimborium.Channels;

public sealed class BCBuffer<TIn, TOut>
    : BCProcessorUnsync<TIn, TOut>
    , IBCMonitored {
    private readonly Func<TIn, IBCConsumer<TOut>, CancellationToken, Task> _AsyncAction;
    private readonly Func<BCError, IBCConsumer<TOut>, CancellationToken, Task>? _OnError;
    private readonly Func<IBCConsumer<TOut>, CancellationToken, Task>? _OnComplete;
    private readonly Channel<TIn> _Channel;
    private Task? _TaskExecution;

    public BCBuffer(
            BCDescription? description,
            Func<TIn, IBCConsumer<TOut>, CancellationToken, Task> asyncAction,
            Func<BCError, IBCConsumer<TOut>, CancellationToken, Task>? onError,
            Func<IBCConsumer<TOut>, CancellationToken, Task>? onComplete,
            IBCConsumer<TOut> next
        ) : base(
            description,
            next
        ) {
        this._AsyncAction = asyncAction;
        this._OnError = onError;
        this._OnComplete = onComplete;
        this._Channel = System.Threading.Channels.Channel.CreateUnbounded<TIn>();
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        try {
            await this._Channel.Writer.WriteAsync(value, cancellationToken);

            if (this._TaskExecution is null) {
                this.StartExecution(cancellationToken);
            }
        } catch (Exception error) {
            BCError bcError = new(error);
            await this.NextConsumer.OnError(bcError, cancellationToken);
            bcError.ThrowIfNotHandled();
        }
    }


    public override async Task OnError(BCError value, CancellationToken cancellationToken) {
        try {
            if (this._OnError is { } onError) {
                await onError(value, this.NextConsumer, cancellationToken).ConfigureAwait(false);
            } else {
                await base.OnError(value, cancellationToken).ConfigureAwait(false);
            }
        } catch (Exception error) {
            BCError bcError = new(error);
            await base.OnError(bcError, cancellationToken).ConfigureAwait(false);
            bcError.ThrowIfNotHandled();
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        if (this.SetCompleting()) {
            this._Channel.Writer.Complete(default);

            if (this._TaskExecution is { } taskExecution) {
                await taskExecution;
            } else {
                if (this._OnComplete is { } onComplete) {
                    await onComplete(this.NextConsumer, cancellationToken);
                }

                await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
            }

        }
    }

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        await this._Channel.Reader.Completion;

        if (this._TaskExecution is { } taskExecution) {
            await taskExecution;
        }

        await this.NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    private void StartExecution(CancellationToken cancellationToken) {
        if (this._TaskExecution is null) {
            lock (this) {
                if (this._TaskExecution is null) {
                    this._TaskExecution = Task.Run(() => this.ExecutionAsync(cancellationToken), cancellationToken);
                }
            }
        }
    }

    private async Task ExecutionAsync(CancellationToken cancellationToken) {
        var reader = this._Channel.Reader;
        while (await reader.WaitToReadAsync(cancellationToken)) {
            while (reader.TryRead(out var valueTIn)) {
                try {
                    await this._AsyncAction(valueTIn, this.NextConsumer, cancellationToken);
                } catch (Exception error) {
                    BCError bcError = new(error);
                    await this.NextConsumer.OnError(bcError, cancellationToken);
                    bcError.ThrowIfNotHandled();
                }
            }
        }
        if (this.SetCompleted()) {
            if (this._OnComplete is { } onComplete) {
                await onComplete(this.NextConsumer, cancellationToken);
            }

            await this.NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
