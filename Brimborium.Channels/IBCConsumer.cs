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
