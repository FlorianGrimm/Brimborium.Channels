namespace Brimborium.Channels;

/// <summary>
/// Terminal consumer that accumulates all received values into a <see cref="List{T}"/>.
/// The collected list is exposed as a <see cref="System.Threading.Tasks.Task{T}"/> that completes when
/// <c>OnComplete</c> is called, or faults when <c>OnError</c> is called.
/// </summary>
/// <typeparam name="T">The type of values collected by this consumer.</typeparam>
public sealed class BCConsumerListValue<T>
    : BCPartMonitored
    , IBCConsumerSubscribable<T>
    , IBCMonitored {
    private readonly TaskCompletionSource<List<T>> _Result = new();
    private readonly List<IBCConnection<T>> _ListConnection = new();
    private readonly List<T> _ListTarget;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description">TODO</param>
    public BCConsumerListValue(
            BCDescription description,
            List<T>? listTarget = null
        ) : base(
            description
        ) {
        this._ListTarget = listTarget ?? new();
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
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
            BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
            this._Result.TrySetResult(this._ListTarget);
            return Task.CompletedTask;
        }
    }

    public Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            this._Result.TrySetException(value.Error);
            value.SetIsHandled();
            return Task.CompletedTask;
        }
    }

    public Task OnNext(T value, CancellationToken cancellationToken) {
        this._ListTarget.Add(value);
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

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public override bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        // no next consumer
        return true;
    }
}
