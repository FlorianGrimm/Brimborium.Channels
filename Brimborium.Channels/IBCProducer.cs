#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

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