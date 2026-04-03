#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// BCProcessorChannelTracking, BCProcessorChannelTrackingNext, _TrackingManager, _NextConsumer
/// BCProcessorChannelTracking.OnNext
///     _CreateRequest
///     _TrackingManager.Add
///     _SendRequest
/// </summary>
/// <typeparam name="TIn"></typeparam>
/// <typeparam name="TOut"></typeparam>
/// <typeparam name="TBCTracking"></typeparam>
public abstract class BCProcessorChannelTracking<TIn, TOut, TBCTracking>
    : BCPartMonitored
    , IBCConsumer<TIn>
    where TBCTracking : BCTracking<TIn, TOut> {
    protected readonly Func<BCDescription, TIn, IBCTrackingManager, IBCConsumer<TOut>, TBCTracking> _CreateRequest;
    private readonly Func<TBCTracking, CancellationToken, Task> _SendRequest;
    protected readonly BCDescription _NextDescription;
    protected readonly IBCConsumer<TOut> _NextConsumer;
    protected readonly BCProcessorChannelTrackingNext _TrackingNext;
    protected readonly BCTrackingManager _TrackingManager;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description">TODO</param>
    /// <param name="channel">TODO</param>
    /// <param name="nextConsumer">TODO</param>
    public BCProcessorChannelTracking(
            BCDescription description,
            Func<
                    BCDescription /*description*/,
                    TIn /*Value*/,
                    IBCTrackingManager /*TrackingManager*/,
                    IBCConsumer<TOut> /*nextConsumer*/,
                    TBCTracking
                > createRequest,
            Func<TBCTracking, CancellationToken, Task> sendRequest,
            IBCConsumer<TOut> nextConsumer
        ) :base(
            description
        ){
        this._CreateRequest = createRequest;
        this._SendRequest = sendRequest;
        this._NextDescription = new BCDescription($"{description.Name}-Next");
        this._NextConsumer = nextConsumer;
        BCTrackingManager trackingManager = new(
            description: new BCDescription($"{description.Name}-Tracking"),
            nextConsumer: nextConsumer
            );
        this._TrackingManager = trackingManager;
        BCProcessorChannelTrackingNext processorTrackingNext = new(
            description: this._NextDescription,
            trackingManager: trackingManager,
            nextConsumer: nextConsumer
            );
        this._TrackingNext = processorTrackingNext;
    }

    /// <summary>
    ///     _CreateRequest
    ///     _TrackingManager.Add
    ///     _SendRequest
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task OnNext(TIn value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            try {
                var tracking = this._CreateRequest(
                    this._NextDescription,
                    value,
                    this._TrackingManager,
                    this._TrackingNext
                    );
                this._TrackingManager.Add(tracking);
                await this._SendRequest(tracking, cancellationToken);
            } catch (Exception error) {
                BCError bcError = new(error);
                await this._NextConsumer.OnError(bcError, cancellationToken);
                bcError.ThrowIfNotHandled();
            }
        }
    }
    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        await this._NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            if (this.SetCompleting()) {
                if (this.SetCompleted()) {
                    await this._TrackingNext.OnComplete(cancellationToken);
                    await this._NextConsumer.OnComplete(cancellationToken);
                }
            }
        }
    }

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            await this._NextConsumer.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(BCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this._NextConsumer);
        }
        return true;
    }

    // Hint
    // SetMonitor(BCMonitor monitor) 
    // BCChannelTrackingNext is NextConsumer

    protected sealed class BCProcessorChannelTrackingNext
        : IBCConsumer<TOut>
        , IBCMonitored {
        private readonly BCTrackingManager _TrackingManager;
        private readonly IBCConsumer<TOut> _NextConsumer;
        private BCMonitor? _Monitor;

        public BCProcessorChannelTrackingNext(
                BCDescription description,
                BCTrackingManager trackingManager,
                IBCConsumer<TOut> nextConsumer
            ){
            this.Description = description;
            this._TrackingManager = trackingManager;
            this._NextConsumer = nextConsumer;
        }

        public BCLifeTime LifeTime => this._TrackingManager.LifeTime;

        public BCDescription Description { get; }

        public async Task OnTrackingComplete(BCTracking<TIn, TOut> tracking, CancellationToken cancellationToken) {
            await this._TrackingManager.OnTrackingComplete(tracking, cancellationToken);
        }

        public async Task OnTrackingError(BCTracking<TIn, TOut> tracking, BCError value, CancellationToken cancellationToken) {
            await this.OnError(value, cancellationToken);
        }

        public async Task OnComplete(CancellationToken cancellationToken) {
            await this._TrackingManager.OnComplete(cancellationToken);
        }

        public Task OnError(BCError value, CancellationToken cancellationToken) {
            return this._NextConsumer.OnError(value, cancellationToken);
        }

        public Task OnNext(TOut value, CancellationToken cancellationToken) {
            return this._NextConsumer.OnNext(value, cancellationToken);
        }

        public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
            await this._TrackingManager.WaitCompletedAsync(cancellationToken);
            await this._NextConsumer.WaitCompletedAsync(cancellationToken);
        }

        BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
        public bool SetMonitor(BCMonitor monitor) {
            if (this._Monitor is { }) { return false; }
            this._Monitor = monitor;
            return true;
        }

    }
}
