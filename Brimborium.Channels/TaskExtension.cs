namespace Brimborium.Channels;
public static partial class TaskExtension {
    extension(Task that) {
        public async Task WriterComplete<TItem>(OwnedChannelWriter<TItem> ownedChannelWriter) {
            try {
                await that.ConfigureAwait(true);
                ownedChannelWriter.CompleteSuccess();
            } catch (Exception error) {
                ownedChannelWriter.CompleteFailed(error);
                throw;
            }
        }
    }

    extension<T>(Task<T> that) {
        public async Task<T> WriterComplete<TItem>(OwnedChannelWriter<TItem> ownedChannelWriter) {
            try {
                var result = await that.ConfigureAwait(true);
                ownedChannelWriter.CompleteSuccess();
                return result;
            } catch (Exception error) {
                ownedChannelWriter.CompleteFailed(error);
                throw;
            }
        }
    }
}
