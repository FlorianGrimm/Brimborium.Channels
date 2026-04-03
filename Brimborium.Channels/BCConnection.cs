#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="T">TODO</typeparam>
public sealed class BCConnection<T>
    :BCPartMonitored
    , IBCConnection<T> {

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
    public BCConnection(
            BCDescription description,
            IBCProducer<T> outgoingProducer,
            IBCConsumer<T> incomingConsumer
        ): base(
            description
        ) {
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

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            await this.RightIncomingConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(BCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.RightIncomingConsumer);
        }
        return true;
    }
}