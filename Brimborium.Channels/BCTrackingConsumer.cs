#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public class BCTrackingConsumer<TIn, TOut>
    : BCPartMonitored
    , IBCTrackingConsumer<BCTracking<TIn, TOut>, TOut> {
    private readonly IBCTrackingManager _TrackingManager;
    private readonly IBCConsumer<BCMessage<TIn, TOut>> _NextConsumer;

    public BCTrackingConsumer(
            BCDescription description,
            IBCTrackingManager trackingManager,
            IBCConsumer<BCMessage<TIn, TOut>> nextConsumer
        ) : base(
            description
        ) {
        this._TrackingManager = trackingManager;
        this._NextConsumer = nextConsumer;
    }

    public async Task OnNext(BCTracking<TIn, TOut> tracking, TOut value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            var message = BCMessage<TIn, TOut>.OnNext(tracking.Value, value);
            await this._NextConsumer.OnNext(message, cancellationToken);
        }
    }

    public async Task OnError(BCTracking<TIn, TOut> tracking, BCError error, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnError")) {
            var message = BCMessage<TIn, TOut>.OnError(tracking.Value, error);
            await this._NextConsumer.OnNext(message, cancellationToken);
            if (this._TrackingManager.RemoveTracking(tracking)) {
                await this._NextConsumer.OnComplete(cancellationToken);
            }
        }
    }

    public async Task OnComplete(BCTracking<TIn, TOut> tracking, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            var message = BCMessage<TIn, TOut>.OnComplete(tracking.Value);
            await this._NextConsumer.OnNext(message, cancellationToken);

            if (this._TrackingManager.RemoveTracking(tracking)) {
                await this._NextConsumer.OnComplete(cancellationToken);
            }
        }
    }

    //public Task WaitRightCompletedAsync(BCTracking<TIn, TOut> tracking, CancellationToken cancellationToken) {
    //    using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
    //        throw new NotImplementedException();
    //    }
    //}

    //public Task WaitSelfCompletedAsync(BCTracking<TIn, TOut> tracking, CancellationToken cancellationToken) {
    //    using (this._Monitor?.LogEnter(this, "WaitSelfCompletedAsync")) {
    //        throw new NotImplementedException();
    //    }
    //}

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitSelfCompletedAsync")) {
            await this._TrackingManager.WaitSelfCompletedAsync(cancellationToken);
        }
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitRightCompletedAsync")) {
            await this._NextConsumer.WaitSelfCompletedAsync(cancellationToken);
            await this._NextConsumer.WaitRightCompletedAsync(cancellationToken);
        }
    }
}