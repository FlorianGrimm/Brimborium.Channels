namespace Brimborium.Channels;

public class CombinedBulkProcess<TInput>
    : System.IDisposable {
    private Channel<TInput>? _Channel;
    private readonly int _MaxCount;
    private readonly CancellationTokenSource _CtsDone = new();

    public CombinedBulkProcess() : this(
            Channel.CreateUnbounded<TInput>(), 
            0
        ) {
    }
    public CombinedBulkProcess(
            int maxCount
        ) : this(
            Channel.CreateUnbounded<TInput>(),
            maxCount
        ) {
    }

    public CombinedBulkProcess(
        Channel<TInput> channel,
        int maxCount
        ) {
        this._Channel = channel;
        this._MaxCount = maxCount;
    }

    public async Task Enqueue(TInput request, CancellationToken cancellationToken) {
        var channel = this._Channel ?? throw new ObjectDisposedException("CombinedBulkProcess");
        await channel.Writer.WriteAsync(request, cancellationToken);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken) {
        var loopToken = CancellationTokenSource.CreateLinkedTokenSource(this._CtsDone.Token, cancellationToken).Token;
        var channel = this._Channel ?? throw new ObjectDisposedException("CombinedBulkProcess");
        var channelReader = channel.Reader;
        List<TInput> listInput = new();
        int maxCount = ((0 < this._MaxCount) ? this._MaxCount : int.MaxValue);
        while (!loopToken.IsCancellationRequested) {
            // continue fast reading until maxCount
            if ((listInput.Count < maxCount)
                && (channelReader.TryRead(out var item))) {
                listInput.Add(item);
                continue;
            }
            if (0 < listInput.Count) {
                await this.ProcessAsync(listInput, loopToken);
                listInput = new();
            }
            var repeat = await channelReader.WaitToReadAsync(loopToken);
            if (repeat) {
                continue;
            } else {
                break;
            }
        }
    }

    protected virtual Task ProcessAsync(
        List<TInput> listInput,
        CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            this._Channel = null;
            this._CtsDone.Cancel();
        } else {
            this._Channel = null;
            this._CtsDone.Dispose();
        }
    }

    ~CombinedBulkProcess() {
        this.Dispose(disposing: false);
    }

    public void Dispose() {
        this.Dispose(disposing: true);
        System.GC.SuppressFinalize(this);
    }
}