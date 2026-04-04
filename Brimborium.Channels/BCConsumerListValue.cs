namespace Brimborium.Channels;

/// <summary>
/// Terminal consumer that accumulates all received values into a <see cref="List{T}"/>.
/// The collected list is exposed as a <see cref="System.Threading.Tasks.Task{T}"/> that completes when
/// <c>OnComplete</c> is called, or faults when <c>OnError</c> is called.
/// </summary>
/// <typeparam name="T">The type of values collected by this consumer.</typeparam>
public sealed class BCConsumerListValue<T>
    : BCPartMonitored
    , IBCConsumerSubscribable<T> {
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
    /// Get the result after OnComplete was called
    /// </summary>
    /// <param name="cancellationToken">stop me</param>
    /// <returns>the result</returns>
    public Task<List<T>> GetResultAsync(CancellationToken cancellationToken) {
        return this._Result.Task.WaitAsync(cancellationToken);
    }

    public Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            this.SetCompleting();
            if (this.GetIsAllConnectionCompleted()) {
                if (this.SetCompleted()) {
                    this._Result.TrySetResult(this._ListTarget);
                }
            }
        }
        return Task.CompletedTask;
    }

    private bool GetIsAllConnectionCompleted() {
        foreach (var connection in this._ListConnection) {
            if (BCLifeTime.Completed != connection.LifeTime) {
                return false;
            }
        }
        return true;
    }

    public Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
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
    /// Waits for OnComplete being called.
    /// </summary>
    /// <param name="cancellationToken">stop me</param>
    /// <returns></returns>
    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        await this._Result.Task;
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        foreach (var connection in this._ListConnection) {
            await connection.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await connection.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
