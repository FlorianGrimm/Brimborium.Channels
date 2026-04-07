#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="TIn1"></typeparam>
/// <typeparam name="TIn1Transformed"></typeparam>
/// <typeparam name="TOut1"></typeparam>
/// <typeparam name="TOut2"></typeparam>
/// <typeparam name="TBCTracking"></typeparam>
public abstract class BCProcessorTrackingO2<TIn1, TIn1Transformed, TOut1, TOut2, TBCTracking>
    : BCPartMonitored
    , IBCConsumer<TIn1>
    where TBCTracking : IBCTracking {

    protected readonly BCDescription NextDescription;

    /// <summary>
    /// BCProcessorTracking --} NextConsumer
    /// </summary>
    protected readonly IBCConsumer<BCMessage<TIn1Transformed, TOut1>> NextConsumer1;
    protected readonly IBCConsumer<BCMessage<TIn1Transformed, TOut2>> NextConsumer2;

    /// <summary>
    /// Tracks trackings
    /// </summary>
    protected readonly BCTrackingManager TrackingManager;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description"></param>
    /// <param name="nextConsumer1"></param>
    /// <param name="nextConsumer2"></param>
    public BCProcessorTrackingO2(
            BCDescription description,
            IBCConsumer<BCMessage<TIn1Transformed, TOut1>> nextConsumer1,
            IBCConsumer<BCMessage<TIn1Transformed, TOut2>> nextConsumer2
        ) : base(
            description
        ) {
        this.NextDescription = new BCDescription($"{description.Name}-Next");
        this.NextConsumer1 = nextConsumer1;
        this.NextConsumer2 = nextConsumer2;
        BCTrackingManager trackingManager = new(
            description: new BCDescription($"{description.Name}-TrackingManager"));

        this.TrackingManager = trackingManager;
    }


    protected abstract TBCTracking CreateRequest(
        TIn1 Value
        );

    protected abstract Task SendRequest(
        TBCTracking tracking,
        CancellationToken cancellationToken);

    public virtual async Task OnNext(TIn1 value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            try {
                var tracking = this.CreateRequest(value);
                this.TrackingManager.AddTracking(tracking);
                await this.SendRequest(tracking, cancellationToken);
            } catch (Exception error) {
                BCError bcError = new(error);
                await this.NextConsumer1.OnError(bcError, cancellationToken);
                bcError.ThrowIfNotHandled();
            }
        }
    }
    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this.NextConsumer1.OnError(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            if (this.SetCompleting()) {
                if (this.SetCompleted()) {
                    if (this.TrackingManager.OnComplete()) {
                        // await this.TrackingConsumer.OnComplete(tracking??, cancellationToken);
                        await this.NextConsumer1.OnComplete(cancellationToken);
                    }
                }
            }
        }
    }

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.WaitRightCompletedAsync))) {
            await this.NextConsumer1.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer1.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.NextConsumer1);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this.NextConsumer1);
    }
}
