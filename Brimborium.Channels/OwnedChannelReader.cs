namespace Brimborium.Channels;

public sealed class OwnedChannelReader<T> : IDisposable {
    private ChannelReader<T>? _ChannelReader;
    private string _Name;

    public ChannelReader<T> ChannelReader {
        get => this._ChannelReader ?? throw new ArgumentNullException(this._Name);
    }

    public OwnedChannelReader(ChannelReader<T> channelReader, string name) {
        this._ChannelReader = channelReader;
        this._Name = name;
    }

    private void Dispose(bool disposing) {
        if (disposing) {
            this._ChannelReader = null;
        } else {
            System.Diagnostics.Debug.WriteLine($"ChannelReader not disposed.");
        }
    }

    ~OwnedChannelReader() {
        this.Dispose(disposing: false);
    }

    public void Dispose() {
        this.Dispose(disposing: true);
        System.GC.SuppressFinalize(this);
    }

    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken) {
        var channelReader = this._ChannelReader
            ?? throw new ObjectDisposedException(this._Name);
        var result = channelReader.ReadAllAsync(cancellationToken);
        this.Dispose();
        return result;
    }

    public IAsyncEnumerable<List<T>> ReadChunkedAsync(
        int maxSize,
        CancellationToken cancellationToken
        ) {
        var channelReader = this._ChannelReader
            ?? throw new ObjectDisposedException(this._Name);
        var result = channelReader.ReadChunkedAsync(maxSize, cancellationToken);
        this.Dispose();
        return result;
    }
}
