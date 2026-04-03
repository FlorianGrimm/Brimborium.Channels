#pragma warning disable IDE1006 // Naming Styles

using System.Threading.Channels;

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="TIn"></typeparam>
/// <typeparam name="TOut"></typeparam>
public sealed class BCBuffer<TIn, TOut>
    : BCProcessorUnsync<TIn, TOut> {
    private readonly Func<TIn, IBCConsumer<TOut>, CancellationToken, Task> _OnNext;
    private readonly Func<BCError, IBCConsumer<TOut>, CancellationToken, Task>? _OnError;
    private readonly Func<IBCConsumer<TOut>, CancellationToken, Task>? _OnComplete;
    private readonly Channel<TIn> _Channel;
    private Task? _TaskExecution;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description">TODO</param>
    /// <param name="onNext">TODO</param>
    /// <param name="onError">TODO</param>
    /// <param name="onComplete">TODO</param>
    /// <param name="channel">TODO</param>
    /// <param name="next">TODO</param>
    public BCBuffer(
            BCDescription description,
            Func<TIn, IBCConsumer<TOut>, CancellationToken, Task> onNext,
            Func<BCError, IBCConsumer<TOut>, CancellationToken, Task>? onError,
            Func<IBCConsumer<TOut>, CancellationToken, Task>? onComplete,
            System.Threading.Channels.Channel<TIn>? channel,
            IBCConsumer<TOut> next
        ) : base(
            description,
            next
        ) {
        this._OnNext = onNext;
        this._OnError = onError;
        this._OnComplete = onComplete;
        this._Channel = channel ?? System.Threading.Channels.Channel.CreateUnbounded<TIn>();
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task OnComplete(CancellationToken cancellationToken) {
        if (this.SetCompleting()) {
            this._Channel.Writer.Complete(default);
            if (this._TaskExecution is null) {
                this.StartExecution(cancellationToken);
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "Channel Completion")) {
            await this._Channel.Reader.Completion.ConfigureAwait(false);
        }

        if (this._TaskExecution is { } taskExecution) {
            using (this._Monitor?.LogEnter(this, "Channel Execution Loop")) {
                await taskExecution.ConfigureAwait(false);
            }
        }

        await this.NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    private void StartExecution(CancellationToken cancellationToken) {
        if (this._TaskExecution is null) {
            lock (this) {
                if (this._TaskExecution is null) {
                    Task.Run(async () => {
                        try {
                            var task = this.ExecutionAsync(cancellationToken);
                            this._TaskExecution = task;
                            await task;
                        } catch (Exception ex) {
                            BCError error = new(ex);
                            await this.OnError(error, cancellationToken);
                            error.ThrowIfNotHandled();
                        } finally {
                            this._TaskExecution = null;
                        }
                    }, cancellationToken);
                }
            }
        }
    }

    private async Task ExecutionAsync(CancellationToken cancellationToken) {
        var reader = this._Channel.Reader;
        while (await reader.WaitToReadAsync(cancellationToken)) {
            while (reader.TryRead(out var valueTIn)) {
                try {
                    await this._OnNext(valueTIn, this.NextConsumer, cancellationToken);
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

            await this.NextConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
        }
    }
}
