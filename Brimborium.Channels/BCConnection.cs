#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="T">TODO</typeparam>
public sealed class BCConnection<T>
    : IBCConnection<T>
    , IBCMonitored {
    private BCLifeTime _LifeTime;
    private BCMonitor? _Monitor;

    /// <summary>
    /// TODO
    /// </summary>
    public BCLifeTime LifeTime => this._LifeTime;

    /// <summary>
    /// TODO
    /// </summary>
    public IBCProducer<T> LeftOutgoingProducer { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public IBCConsumer<T> RightIncomingConsumer { get; }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="outgoingProducer">TODO</param>
    /// <param name="incomingConsumer">TODO</param>
    public BCConnection(IBCProducer<T> outgoingProducer, IBCConsumer<T> incomingConsumer) {
        this.LeftOutgoingProducer = outgoingProducer;
        this.RightIncomingConsumer = incomingConsumer;
    }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
                BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
                await this.RightIncomingConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            await this.RightIncomingConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task OnNext(T value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            await this.RightIncomingConsumer.OnNext(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            await this.RightIncomingConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        monitor.Add(this.RightIncomingConsumer);
        return true;
    }
}