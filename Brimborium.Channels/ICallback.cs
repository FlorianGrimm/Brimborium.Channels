namespace Brimborium.Channels;

public interface ICallback<T> {
    ValueTask ExecuteAsync(T value, CancellationToken cancellationToken);
}

public sealed record CallbackPair<T>(
    Action<T>? Callback,
    Func<T, CancellationToken, Task>? AsyncCallback
    ) : ICallback<T> {
    public CallbackPair(Action<T>? Callback) : this(Callback, default) { }
    public CallbackPair(Func<T, CancellationToken, Task>? AsyncCallback) : this(default, AsyncCallback) { }

    public async ValueTask ExecuteAsync(T value, CancellationToken cancellationToken) {
        if (this.Callback is { } callback) {
            callback(value);
        }
        if (this.AsyncCallback is { } asyncCallback) {
            await asyncCallback(value, cancellationToken);
        }
    }
}

public sealed record class CallbackToChannel<T>(
    SharedChannelWriter<T> ChannelWriter
    )
    : ICallback<T> {
    public async ValueTask ExecuteAsync(T value, CancellationToken cancellationToken) {
        await this.ChannelWriter.WriteAsync(value, cancellationToken);
    }
}