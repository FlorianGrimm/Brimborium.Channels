#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
public enum BCLifeTime {
    /// <summary>
    /// After creation Subscripe OnNext OnError does not change this.
    /// </summary>
    Active,

    /// <summary>
    /// OnComplete was called, may be other Incoming Connections are still Active, actions may be still pending.
    /// </summary>
    Completing,

    /// <summary>
    /// If Completing and all work is done. OnComplete to the next is being send or was send.
    /// </summary>
    Completed
}

public static class BCLifeTimeExtension {
    /// <summary>
    /// TODO
    /// </summary>
    public static bool SetCompleting(ref BCLifeTime lifeTimeField) {
        return (BCLifeTime.Active == lifeTimeField)
            && (BCLifeTime.Active == System.Threading.Interlocked.CompareExchange(ref lifeTimeField, BCLifeTime.Completing, BCLifeTime.Active));
    }

    /// <summary>
    /// TODO
    /// </summary>
    public static bool SetCompleted(ref BCLifeTime lifeTimeField) {
        return (BCLifeTime.Completing == lifeTimeField)
            && (BCLifeTime.Completing == System.Threading.Interlocked.CompareExchange(ref lifeTimeField, BCLifeTime.Completed, BCLifeTime.Completing));
    }
}

/// <summary>
/// TODO
/// </summary>
public interface IBCPart {
    /// <summary>
    /// The current lifetime state
    /// </summary>
    BCLifeTime LifeTime { get; }

    /// <summary>
    /// The state switched to Complete.
    /// </summary>
    Task WaitCompletedAsync(CancellationToken cancellationToken);

}
/// <summary>
/// TODO
/// </summary>
public interface IBCConsumer : IBCPart {
}

/// <summary>
/// TODO
/// </summary>
public interface IBCConsumer<T> : IBCConsumer {

    /// <summary>Receives the next value in the stream.</summary>
    Task OnNext(T value, CancellationToken cancellationToken);

    /// <summary>Signals that an error has occurred in the stream.</summary>
    Task OnError(BCError value, CancellationToken cancellationToken);

    /// <summary>Signals that the stream has completed with no more values to produce.</summary>
    /// <remarks>You can call this more than one.</remarks>
    Task OnComplete(CancellationToken cancellationToken);
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