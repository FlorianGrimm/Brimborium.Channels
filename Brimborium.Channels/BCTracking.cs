#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>Non-generic marker interface for an in-flight tracking unit; exposes its unique id.</summary>
public interface IBCTracking {
    long GetId();
}

/*
    /// <summary>
    /// Typed tracking interface that combines consumer signals, monitoring support,
    /// and the non-generic <see cref="IBCTracking"/> id marker.
    /// </summary>
    /// <typeparam name="TOut">The type of output values this tracking unit can receive.</typeparam>
    public interface IBCTrackingOut<TOut>
        : IBCConsumer<TOut>
        , IBCMonitored
        , IBCTracking {
    }
*/

public interface IBCTrackingIn<TIn>
    : IBCTracking {
    TIn Value { get; }
}

/*
    public interface IBCTrackingConsumer<TBCTracking, TOut>
        : IBCMonitored
        where TBCTracking : IBCTracking {
        Task OnNext(TBCTracking tracking, TOut value, CancellationToken cancellationToken);
        Task OnError(TBCTracking tracking, BCError error, CancellationToken cancellationToken);
        Task OnComplete(TBCTracking tracking, CancellationToken cancellationToken);
        //not so easy - is this needed?
        //Task WaitSelfCompletedAsync(TBCTracking tracking, CancellationToken cancellationToken);
        //Task WaitRightCompletedAsync(TBCTracking tracking, CancellationToken cancellationToken);
    }

    public interface IBCTracking<TIn, TOut>
        : IBCTrackingOut<TOut>
        , IBCTrackingIn<TIn> {
    }


    public readonly record struct BCTracking(long Id) : IBCTracking {
        public readonly long GetId() => this.Id;
    }
*/
public abstract class BCTracking
    : BCPartMonitored
    , IBCConsumer
    , IBCTracking {
    private static long _NextId;
    internal readonly long Id;

    protected BCTracking(
            BCDescription description
        ) : base(
            description
        ) {
        this.Id = System.Threading.Interlocked.Increment(ref _NextId);
    }

    public long GetId() => this.Id;

    public virtual Task OnError(BCError value, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public virtual Task OnComplete(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

}
