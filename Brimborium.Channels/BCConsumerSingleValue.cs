#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Terminal consumer that captures the last value received from the stream.
/// The captured value is exposed as a <see cref="System.Threading.Tasks.Task{T}"/> that completes when
/// <c>OnComplete</c> is called, or faults when <c>OnError</c> is called.
/// If no value arrived before completion, the task is cancelled.
/// </summary>
/// <typeparam name="T">The type of value captured by this consumer.</typeparam>
public sealed class BCConsumerSingleValue<T>
    : BCPartMonitored
    , IBCConsumerSubscribable<T> {
    private readonly TaskCompletionSource<T?> _Result = new();
    private readonly List<IBCConnection<T>> _ListConnection = new();
    private T _Value = default!;
    private bool _HasValue;
    private Exception? _Error;
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description"></param>
    public BCConsumerSingleValue(
            BCDescription description
        ) : base(
            description
        ) {
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <returns></returns>
    public Task<T?> GetResultAsync() {
        return this._Result.Task;
    }

    public Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
            if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                if (this._HasValue) {
                    this._Result.TrySetResult(this._Value);
                } else if (this._Error is { } error) {
                    this._Result.TrySetException(error);
                } else {
                    this._Result.TrySetCanceled(CancellationToken.None);
                }
            }
            return Task.CompletedTask;
        }
    }

    public Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            this._Error = value.Error;
            value.SetIsHandled();
            return Task.CompletedTask;
        }
    }

    public Task OnNext(T value, CancellationToken cancellationToken) {
        this._HasValue = true;
        this._Value = value;
        return Task.CompletedTask;
    }

    public Task OnSubscribe(IBCConnection<T> connection, CancellationToken cancellationToken) {
        this._ListConnection.Add(connection);
        return Task.CompletedTask;
    }
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        await this._Result.Task;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        foreach (var connection in this._ListConnection) {
            await connection.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await connection.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}