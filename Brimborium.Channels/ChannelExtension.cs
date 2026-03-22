namespace Brimborium.Channels;

public static class ChannelExtension {
    extension<T>(Channel<T> channel) {
        public OwningChannel<T> AsOwningChannel(string name)
            => new(channel, name);
    }

    extension<T>(ChannelReader<T> channelReader) {
        /// <summary>
        /// Read the channel into chunks with maximal size of <paramref name="maxSize"/>.
        /// </summary>
        /// <param name="maxSize">the maximum size of a chunk</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>the channel as chunks</returns>
        public async IAsyncEnumerable<List<T>> ReadChunkedAsync(int maxSize, [EnumeratorCancellation] CancellationToken cancellationToken) {
            if (maxSize <= 0) {
                maxSize = int.MaxValue;
            }
            List<T> buffer = new(maxSize);
            while (!cancellationToken.IsCancellationRequested) {
                while ((buffer.Count < maxSize)
                    && (channelReader.TryRead(out var item))) {
                    buffer.Add(item);
                }
                if (0 < buffer.Count) {
                    yield return buffer;
                    buffer = new(maxSize);
                }
                var isContinued = await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(true);
                if (isContinued) {
                    continue;
                } else {
                    yield break;
                }
            }
        }
    }
}
