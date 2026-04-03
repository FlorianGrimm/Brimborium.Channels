#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
public interface IBCConsumer : IBCPart {
    /// <summary>Signals that an error has occurred in the stream.</summary>
    Task OnError(BCError value, CancellationToken cancellationToken);

    /// <summary>Signals that the stream has completed with no more values to produce.</summary>
    /// <remarks>You can call this more than one.</remarks>
    Task OnComplete(CancellationToken cancellationToken);
}

/// <summary>
/// TODO
/// </summary>
public interface IBCConsumer<T> : IBCConsumer {
    /// <summary>Receives the next value in the stream.</summary>
    Task OnNext(T value, CancellationToken cancellationToken);
}

/// <summary>
/// TODO
/// </summary>
public interface IBCConsumerSubscribable<T> : IBCConsumer<T>, IBCPart {
    Task OnSubscribe(IBCConnection<T> connection, CancellationToken cancellationToken);
}

/// <summary>
/// TODO
/// </summary>
public interface IBCProducer : IBCPart {
}

/// <summary>
/// TODO
/// </summary>
public interface IBCProducer<T> : IBCProducer {
    Task<IBCConnection<T>> Subscribe(IBCConsumerSubscribable<T> next, CancellationToken cancellationToken);
}

/// <summary>
/// TODO
/// </summary>
public interface IBCBlock : IBCPart {
}


/// <summary>
/// TODO
/// </summary>
public interface IBCConnection<T> : IBCConsumer<T> {
    IBCProducer<T> LeftOutgoingProducer { get; }
    IBCConsumer<T> RightIncomingConsumer { get; }
}

/// <summary>
/// TODO
/// </summary>
public interface IBCMonitored : IBCPart {
    BCMonitor? GetMonitor();
    /// <summary>
    /// Set the monitor - cascade to the (right) consumer.
    /// </summary>
    /// <param name="monitor">the monitor</param>
    /// <returns>true if set - this is usefull if you override this</returns>
    bool SetMonitor(BCMonitor monitor);
}