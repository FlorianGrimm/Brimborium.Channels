#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Non-generic base for all pipeline consumers.
/// Provides error and completion signals shared by every consumer regardless of value type.
/// </summary>
public interface IBCConsumer : IBCPart {
    /// <summary>Signals that an error has occurred in the stream.</summary>
    /// <param name="value">The error to propagate downstream.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task OnError(BCError value, CancellationToken cancellationToken);

    /// <summary>Signals that the stream has completed with no more values to produce.</summary>
    /// <remarks>Safe to call more than once; only the first call takes effect.</remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task OnComplete(CancellationToken cancellationToken);
}

/// <summary>
/// Typed pipeline consumer that receives a stream of <typeparamref name="T"/> values.
/// </summary>
/// <typeparam name="T">The type of values this consumer accepts.</typeparam>
public interface IBCConsumer<T> : IBCConsumer {
    /// <summary>Receives the next value in the stream.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task OnNext(T value, CancellationToken cancellationToken);
}

/// <summary>
/// A typed consumer that can be connected to an <see cref="IBCProducer{T}"/> via a subscription.
/// Receives the <see cref="IBCConnection{T}"/> handle when a producer subscribes to it.
/// </summary>
/// <typeparam name="T">The type of values this consumer accepts.</typeparam>
public interface IBCConsumerSubscribable<T> : IBCConsumer<T>, IBCPart {
    /// <summary>Called by the producer when this consumer is subscribed to it.</summary>
    /// <param name="connection">The connection handle representing the link between producer and consumer.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task OnSubscribe(IBCConnection<T> connection, CancellationToken cancellationToken);
}

/// <summary>
/// Non-generic marker interface for pipeline producers.
/// </summary>
public interface IBCProducer : IBCPart {
}

/// <summary>
/// Typed pipeline producer that pushes a stream of <typeparamref name="T"/> values to subscribers.
/// </summary>
/// <typeparam name="T">The type of values this producer emits.</typeparam>
public interface IBCProducer<T> : IBCProducer {
    /// <summary>Connects this producer to <paramref name="next"/> and returns the resulting connection handle.</summary>
    /// <param name="next">The consumer to subscribe.</param>
    /// <param name="cancellationToken">Token to cancel the subscribe operation.</param>
    /// <returns>The <see cref="IBCConnection{T}"/> representing the live link between this producer and the consumer.</returns>
    Task<IBCConnection<T>> Subscribe(IBCConsumerSubscribable<T> next, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for composite pipeline blocks that group
/// one or more incoming consumers and outgoing producers behind a single unit.
/// </summary>
public interface IBCBlock : IBCPart {
}

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
/// Extended <see cref="IBCPart"/> that supports attaching a <see cref="BCMonitor"/> for logging and diagnostics.
/// </summary>
public interface IBCMonitored : IBCPart {
    /// <summary>Returns the currently attached monitor, or <c>null</c> if none has been set.</summary>
    BCMonitor? GetMonitor();

    /// <summary>
    /// Attaches a monitor to this part and cascades it to downstream (right-side) consumers.
    /// </summary>
    /// <param name="monitor">The monitor to attach.</param>
    /// <returns><c>true</c> if the monitor was set; <c>false</c> if one was already attached.</returns>
    bool SetMonitor(BCMonitor monitor);
}