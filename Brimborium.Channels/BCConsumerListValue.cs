namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="T">TODO</typeparam>
public sealed class BCConsumerListValue<T> 
    : IBCConsumerSubscribable<T> 
    , IBCMonitored{
    private TaskCompletionSource<List<T>> _Result = new();
    private readonly List<IBCConnection<T>> _ListConnection = new();
    private readonly List<T> _ListTarget = new();
    private BCMonitor? _Monitor;
    private BCLifeTime _LifeTime;

    /// <summary>
    /// TODO
    /// </summary>
    public BCLifeTime LifeTime => this._LifeTime;

    /// <summary>
    /// TODO
    /// </summary>
    public BCDescription Description { get;  set; }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description">TODO</param>
    public BCConsumerListValue(
        BCDescription? description
    ) {
        this.Description=description??new();
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken">TODO</param>
    /// <returns>TODO</returns>
    public Task<List<T>> GetResultAsync(CancellationToken cancellationToken) {
        return this._Result.Task.WaitAsync(cancellationToken);
    }

    public Task OnComplete(CancellationToken cancellationToken) {
        BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
        BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
        this._Result.TrySetResult(this._ListTarget);
        return Task.CompletedTask;
    }

    public Task OnError(BCError value, CancellationToken cancellationToken) {
        this._Result.TrySetException(value.Error);
        value.SetIsHandled();
        return Task.CompletedTask;
    }

    public Task OnNext(T value, CancellationToken cancellationToken) {
        this._ListTarget.Add(value);
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
