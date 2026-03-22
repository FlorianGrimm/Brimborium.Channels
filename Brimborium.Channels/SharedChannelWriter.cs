namespace Brimborium.Channels;

public sealed class SharedChannelWriter<T> : OwningChannelWriter<T> {
    internal SharedChannelWriter(ChannelWriter<T> channelWriter, string name)
        : base(channelWriter, name) {
    }
}
