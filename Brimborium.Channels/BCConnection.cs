#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Represents a live, typed connection between an <see cref="IBCProducer{T}"/> (left side)
/// and an <see cref="IBCConsumer{T}"/> (right side).
/// Acts as a pass-through consumer that forwards signals to the right-side consumer.
/// </summary>
/// <typeparam name="T">The type of values flowing through the connection.</typeparam>
public interface IBCConnection<T> : IBCConsumer<T> {
    /// <summary>The producer on the left (upstream) side of this connection.</summary>
    IBCProducer<T> LeftOutgoingProducer { get; }

    /// <summary>The consumer on the right (downstream) side of this connection.</summary>
    IBCConsumer<T> RightIncomingConsumer { get; }
}

/// <summary>
/// Concrete implementation of <see cref="IBCConnection{T}"/>.
/// Represents the live, typed link between an <see cref="IBCProducer{T}"/> (left side)
/// and an <see cref="IBCConsumer{T}"/> (right side), forwarding all signals to the right consumer.
/// </summary>
/// <typeparam name="T">The type of values flowing through this connection.</typeparam>
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

    // not used
    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            await this.RightIncomingConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);

            await this.RightIncomingConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.RightIncomingConsumer);
        }
        return true;
    }
}