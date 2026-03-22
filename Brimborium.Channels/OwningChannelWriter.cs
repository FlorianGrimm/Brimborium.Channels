namespace Brimborium.Channels;

public abstract class OwningChannelWriter<T> : IDisposable {
    protected ChannelWriter<T>? _ChannelWriter;
    protected readonly string _Name;

    public ChannelWriter<T> ChannelWriter {
        get => this._ChannelWriter ?? throw new ArgumentNullException(this._Name);
    }

    protected OwningChannelWriter(
        ChannelWriter<T> channelWriter,
        string name
        ) {
        this._ChannelWriter = channelWriter;
        this._Name = name;
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            this._ChannelWriter = null;
        }
    }

    ~OwningChannelWriter() {
        this.Dispose(disposing: false);
    }

    public void Dispose() {
        this.Dispose(disposing: true);
        System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="item"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ObjectDisposedException">it's disposed</exception>
    public ValueTask WriteAsync(T item, CancellationToken cancellationToken) {
        var channelWriter = this._ChannelWriter
            ?? throw new ObjectDisposedException(this._Name);
        return channelWriter.WriteAsync(item, cancellationToken);
    }

    /// <summary>
    /// Complete with Error
    /// </summary>
    /// <param name="error">the exception</param>
    /// <exception cref="ObjectDisposedException">it's disposed</exception>
    public void CompleteFailed(Exception error) {
        if (this._ChannelWriter is { } channelWriter) {
            channelWriter.TryComplete(error);
            this.Dispose();
        }
    }
}
