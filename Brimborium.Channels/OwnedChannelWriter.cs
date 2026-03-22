namespace Brimborium.Channels;

public sealed class OwnedChannelWriter<T> : OwningChannelWriter<T> {
    internal OwnedChannelWriter(ChannelWriter<T> channelWriter, string name)
        : base(channelWriter, name) {
    }

    public SharedChannelWriter<T> AsShared() {
        if (this._ChannelWriter is not { } channelWriter) {
            throw new ObjectDisposedException(this._Name);
        }
        return new SharedChannelWriter<T>(channelWriter, this._Name);
    }

    public void CompleteSuccess() {
        if (this._ChannelWriter is { } channelWriter) {
            channelWriter.TryComplete();
            this.Dispose();
        }
    }


    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._ChannelWriter?.TryComplete();
            this._ChannelWriter = null;
        } else {
            System.Diagnostics.Debug.WriteLine($"Not disposed: {this._Name}");
        }
    }
}
