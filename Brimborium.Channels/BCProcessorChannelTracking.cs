#pragma warning disable IDE1006 // Naming Styles

using System.Threading.Channels;

namespace Brimborium.Channels;

/// <summary>
/// Abstract processor that wraps each incoming value in a <see cref="BCTracking{TIn,TOut}"/> unit
/// and dispatches it asynchronously via <c>SendRequest</c> into a channel.
/// A <see cref="BCTrackingManager"/> keeps count of in-flight trackings and delays the <c>OnComplete</c>
/// signal to the downstream consumer until all of them have reported back.
/// Subclasses implement <c>CreateRequest</c> to construct the tracking object and
/// <c>SendRequest</c> to dispatch it.
/// </summary>
/// <typeparam name="TIn">The type of values received from upstream.</typeparam>
/// <typeparam name="TOut1">The type of values forwarded to the downstream consumer.</typeparam>
/// <typeparam name="TBCTracking">The concrete <see cref="BCTracking{TIn,TOut}"/> subtype used by this processor.</typeparam>
public abstract class BCProcessorChannelTracking<TIn, TOut1, TBCTracking>
    : BCPartMonitored
    , IBCConsumer<TIn>
    where TBCTracking : IBCTracking {

    protected readonly BCDescription NextDescription;
    private readonly Channel<TBCTracking> _Channel;

    /// <summary>
    /// NextTrackingConsumer --} NextConsumer
    /// </summary>
    protected readonly IBCConsumer<BCMessage<TIn, TOut1>> NextConsumer;

    /// <summary>
    /// Tracks trackings
    /// </summary>
    protected readonly BCTrackingManager TrackingManager;

    public BCProcessorChannelTracking(
            BCDescription description,
            System.Threading.Channels.Channel<TBCTracking> channel,
            IBCConsumer<BCMessage<TIn, TOut1>> nextConsumer
        ) : base(
            description
        ) {
        this.NextDescription = new BCDescription($"{description.Name}-Next");
        this._Channel = channel;
        this.NextConsumer = nextConsumer;
        BCTrackingManager trackingManager = new(
            description: new BCDescription($"{description.Name}-TrackingManager"));

        this.TrackingManager = trackingManager;
    }


    protected abstract TBCTracking CreateRequest(
        TIn Value
        );

    protected virtual async Task SendRequest(
        TBCTracking tracking,
        CancellationToken cancellationToken) { 
        await this._Channel.Writer.WriteAsync(tracking, cancellationToken);
    }

    public virtual async Task OnNext(TIn value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            try {
                var tracking = this.CreateRequest(value);
                this.TrackingManager.AddTracking(tracking);
                await this.SendRequest(tracking, cancellationToken);
            } catch (Exception error) {
                BCError bcError = new(error);
                await this.NextConsumer.OnError(bcError, cancellationToken);
                bcError.ThrowIfNotHandled();
            }
        }
    }
    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnError))) {
            await this.NextConsumer.OnError(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            if (this.SetCompleting()) {
                if (this.SetCompleted()) {
                    if (this.TrackingManager.OnComplete()) {
                        // await this.TrackingConsumer.OnComplete(tracking??, cancellationToken);
                        await this.NextConsumer.OnComplete(cancellationToken);
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
            await this.NextConsumer.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.NextConsumer);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this.NextConsumer);
    }
}


public abstract class BCProcessorChannelTracking<TIn, TOut1, TOut2, TBCTracking>
    : BCPartMonitored
    , IBCConsumer<TIn>
    where TBCTracking : IBCTracking {

    protected readonly BCDescription NextDescription;
    private readonly Channel<TBCTracking> _Channel;

    /// <summary>
    /// NextTrackingConsumer --} NextConsumer
    /// </summary>
    protected readonly IBCConsumer<BCMessage<TIn, TOut1>> NextConsumer1;
    protected readonly IBCConsumer<BCMessage<TIn, TOut2>> NextConsumer2;

    /// <summary>
    /// Tracks trackings
    /// </summary>
    protected readonly BCTrackingManager TrackingManager;

    public BCProcessorChannelTracking(
            BCDescription description,
            System.Threading.Channels.Channel<TBCTracking> channel,
            IBCConsumer<BCMessage<TIn, TOut1>> nextConsumer1,
            IBCConsumer<BCMessage<TIn, TOut2>> nextConsumer2
        ) : base(
            description
        ) {
        this.NextDescription = new BCDescription($"{description.Name}-Next");
        this._Channel = channel;
        this.NextConsumer1 = nextConsumer1;
        this.NextConsumer2 = nextConsumer2;
        BCTrackingManager trackingManager = new(
            description: new BCDescription($"{description.Name}-TrackingManager"));

        this.TrackingManager = trackingManager;
    }


    protected abstract TBCTracking CreateRequest(
        TIn Value
        );

    protected virtual async Task SendRequest(
        TBCTracking tracking,
        CancellationToken cancellationToken) {
        await this._Channel.Writer.WriteAsync(tracking, cancellationToken);
    }

    public virtual async Task OnNext(TIn value, CancellationToken cancellationToken) {
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
            await this.NextConsumer1.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.WaitSelfCompletedAsync(cancellationToken).ConfigureAwait(false);

            await this.NextConsumer1.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
            await this.NextConsumer2.WaitRightCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            monitor.Add(this.NextConsumer1);
            monitor.Add(this.NextConsumer2);
        }
        return true;
    }

    public override void Describe(BCDescriptionNode node, BCDescriptionGraph description) {
        base.Describe(node, description);
        node.AddOutgoing(this.NextConsumer1);
        node.AddOutgoing(this.NextConsumer2);
    }
}
