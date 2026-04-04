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
    : BCPartMonitored
    , IBCConnection<T> {
    private readonly SemaphoreSlim _Semaphore = new(1, 1);

    /// <summary>
    /// Block.LeftOutgoingProducer --} BCConnection --} Block.RightIncomingConsumer
    /// </summary>
    public IBCProducer<T> LeftOutgoingProducer { get; }

    /// <summary>
    /// Block.LeftOutgoingProducer --} BCConnection --} Block.RightIncomingConsumer
    /// </summary>
    public IBCConsumer<T> RightIncomingConsumer { get; }

    /// <summary>
    /// Block.LeftOutgoingProducer --} BCConnection --} Block.RightIncomingConsumer
    /// </summary>
    /// <param name="outgoingProducer">the left block.outgoingProducer</param>
    /// <param name="incomingConsumer">the right block.incomingConsumer</param>
    public BCConnection(
            BCDescription description,
            IBCProducer<T> outgoingProducer,
            IBCConsumer<T> incomingConsumer
        ) : base(
            description
        ) {
        this.LeftOutgoingProducer = outgoingProducer;
        this.RightIncomingConsumer = incomingConsumer;
    }

    public async Task OnNext(T value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
                await this.RightIncomingConsumer.OnNext(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
            await this.RightIncomingConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
            } finally {
                this._Semaphore.Release();
            }
        }
    }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            if (this.SetCompleting()) {
                if (this.SetCompleted()) {
                    await this._Semaphore.WaitAsync(cancellationToken);
                    try {
                        await this.RightIncomingConsumer.OnComplete(cancellationToken).ConfigureAwait(false);
                    } finally {
                        this._Semaphore.Release();
                    }
                }
            }
        }
    }

    // not used
    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        await this._Semaphore.WaitAsync(cancellationToken);
        this._Semaphore.Release();
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitRightCompletedAsync))) {
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

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        node.Kind = "Connection";
        node.Name = this.Description.Name;
        node.AddIncoming(this.LeftOutgoingProducer);
        node.AddOutgoing(this.RightIncomingConsumer);
    }
}