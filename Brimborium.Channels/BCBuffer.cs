#pragma warning disable IDE1006 // Naming Styles

using System.Threading.Channels;

namespace Brimborium.Channels;

/// <summary>
/// A processor that decouples the upstream producer from the downstream consumer
/// by writing incoming values into a <see cref="System.Threading.Channels.Channel{T}"/>
/// and draining them asynchronously in a background loop.
/// User-supplied delegates handle <c>OnNext</c>, and optionally <c>OnError</c> and <c>OnComplete</c>.
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut">The type of values forwarded to the downstream consumer.</typeparam>
public sealed class BCBuffer<TIn, TOut>
    : BCProcessorSyncedI1O1<TIn, TOut> {
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
    /// <param name="cancellationToken">stop me</param>
    /// <returns></returns>
    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                await this._Channel.Writer.WriteAsync(value, cancellationToken);

                if (this._TaskExecution is null) {
                    this.StartExecution(cancellationToken);
                }
            } catch (Exception error) {
                BCError bcError = new(error);
                await this.NextConsumer.OnError(bcError, cancellationToken);
                bcError.ThrowIfNotHandled();
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken">stop me</param>
    /// <returns></returns>
    public override async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken">stop me</param>
    /// <returns></returns>
    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (this.SetCompleting()) {
                    this._Channel.Writer.Complete(default);
                    if (this._TaskExecution is null) {
                        this.StartExecution(cancellationToken);
                    }
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken">stop me</param>
    /// <returns></returns>
    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitSelfCompletedAsync))) {
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                this.StartExecution(cancellationToken);
            } finally {
                this._Semaphore.Release();
            }
            using (this._Monitor?.LogEnter(this, "Channel Completion")) {
                await this._Channel.Reader.Completion.ConfigureAwait(false);
            }

            using (this._Monitor?.LogEnter(this, "Channel Execution Loop")) {
                if (this._TaskExecution is { } taskExecution) {
                    await taskExecution.WaitAsync(cancellationToken);
                }
                await this._Completion.Task.ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken">stop me</param>
    /// <returns></returns>
    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitRightCompletedAsync))) {
            await this.NextConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void StartExecution(CancellationToken cancellationToken) {
        if (this._TaskExecution is null) {
            lock (this) {
                if (this._TaskExecution is null) {
                    this._TaskExecution = StartExecutionAsync(cancellationToken);
                }
            }
        }

        async Task StartExecutionAsync(CancellationToken cancellation) {
            try {
                var task = this.ExecutionLoop(cancellationToken);
                this._TaskExecution = task;
                await task;
            } catch (Exception ex) {
                BCError error = new(ex);
                await this.OnError(error, cancellationToken);
                error.ThrowIfNotHandled();
            }

        }
    }

    private readonly TaskCompletionSource _Completion = new();
    private async Task ExecutionLoop(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.ExecutionLoop))) {
            try {
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

                    this._Completion.TrySetResult();
                }
            } catch (Exception ex) {
                this._Completion.TrySetException(ex);
            }
        }
    }
}
