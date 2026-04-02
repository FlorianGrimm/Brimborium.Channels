#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="T">TODO</typeparam>
public sealed class BCConsumerSingleValue<T> 
    : IBCConsumerSubscribable<T>
    , IBCMonitored {
    private readonly TaskCompletionSource<T?> _Result = new();
    private readonly List<IBCConnection<T>> _ListConnection = new();
    private T _Value = default!;
    private bool _HasValue;
    private BCLifeTime _LifeTime;
    private Exception? _Error;
    private BCMonitor? _Monitor;

    /// <summary>
    /// TODO
    /// </summary>
    public BCLifeTime LifeTime => this._LifeTime;

    /// <summary>
    /// TODO
    /// </summary>
    public BCDescription Description { get; set; }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description"></param>
    public BCConsumerSingleValue(
        BCDescription? description
    ) {
        this.Description = description ?? new();
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <returns></returns>
    public Task<T?> GetResultAsync() {
        return this._Result.Task;
    }

    public Task OnComplete(CancellationToken cancellationToken) {
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

    public Task OnError(BCError value, CancellationToken cancellationToken) {
        this._Error = value.Error;
        value.SetIsHandled();
        return Task.CompletedTask;
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

    public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        foreach (var connection in this._ListConnection) {
            await connection.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
        await this._Result.Task;
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        // no next consumer
        return true;
    }
}