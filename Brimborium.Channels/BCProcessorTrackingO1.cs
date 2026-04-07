#pragma warning disable IDE1006 // Naming Styles

using System.Diagnostics;

namespace Brimborium.Channels;

/// <summary>
/// Abstract processor that wraps each incoming value in a <see cref="BCTrackingO1{TIn,TOut}"/> unit
/// and dispatches it asynchronously via <c>SendRequest</c>.
/// A <see cref="BCTrackingManager"/> keeps count of in-flight trackings and delays the <c>OnComplete</c>
/// signal to the downstream consumer until all of them have reported back.
/// Subclasses implement <c>CreateRequest</c> to construct the tracking object and
/// <c>SendRequest</c> to dispatch it.
/// </summary>
/// <typeparam name="TIn1">The type of values received from upstream.</typeparam>
/// <typeparam name="TIn1Transformed">Translated TIn1</typeparam>
/// <typeparam name="TOut1">The type of values forwarded to the downstream consumer.</typeparam>
/// <typeparam name="TBCTracking"></typeparam>
public abstract class BCProcessorTrackingO1<TIn1, TIn1Transformed, TOut1, TBCTracking>
    : BCPartMonitored
    , IBCConsumer<TIn1>
    where TBCTracking : IBCTracking {

    protected readonly BCDescription NextDescription;

    /// <summary>
    /// BCProcessorTracking --} NextConsumer
    /// </summary>
    protected readonly IBCConsumer<BCMessage<TIn1Transformed, TOut1>> NextConsumer1;

    /// <summary>
    /// Tracks trackings
    /// </summary>
    protected readonly BCTrackingManager TrackingManager;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description">TODO</param>
    /// <param name="channel">TODO</param>
    /// <param name="nextConsumer1">TODO</param>
    public BCProcessorTrackingO1(
            BCDescription description,
            IBCConsumer<BCMessage<TIn1Transformed, TOut1>> nextConsumer1
        ) : base(
            description
        ) {
        this.NextDescription = new BCDescription($"{description.Name}-Next");
        this.NextConsumer1 = nextConsumer1;
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
