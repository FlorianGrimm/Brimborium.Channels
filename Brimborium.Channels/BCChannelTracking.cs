#pragma warning disable IDE1006 // Naming Styles
#if false
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
/// <typeparam name="TIn"></typeparam>
/// <typeparam name="TOut"></typeparam>
public sealed class BCChannelTracking<TIn, TOut>
    : BCProcessorUnsync<TIn, TOut>
    , IBCMonitored {
    private readonly Channel<BCTracking<TIn, TOut>> _Channel;
    private readonly BCChannelTrackingNext _ChannelTrackingNext;
    private readonly BCDescription _NextDescription;
    private readonly BCTrackingManager _TrackingManager;

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="description">TODO</param>
    /// <param name="channel">TODO</param>
    /// <param name="next">TODO</param>
    public BCChannelTracking(
            BCDescription description,
            Channel<BCTracking<TIn, TOut>>? channel,
            IBCConsumer<TOut> next
        ) : this(
            description: description,
            channel: channel,
            channelTrackingNext: 
                new(
                    description: new BCDescription($"{description.Name}-next"),
                    next: next)
        ) {
    }

    private BCChannelTracking(
            BCDescription description,
            Channel<BCTracking<TIn, TOut>>? channel,
            BCChannelTrackingNext channelTrackingNext
        ) : base(
            description,
            channelTrackingNext
        ) {
        this._Channel = channel ?? System.Threading.Channels.Channel.CreateUnbounded<BCTracking<TIn, TOut>>();
        this._ChannelTrackingNext = channelTrackingNext;
        this._NextDescription = new BCDescription($"{description.Name}-Next");
        this._TrackingManager = new(
            new BCDescription("tracking"),
            channelTrackingNext);
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            try {
                var tracking = new BCTracking<TIn, TOut>(
                    this._NextDescription,
                    value, 
                    this._ChannelTrackingNext, 
                    this._ChannelTrackingNext);
                this._ChannelTrackingNext.AddTracking(tracking);
                await this._Channel.Writer.WriteAsync(
                    tracking,
                    cancellationToken);
            } catch (Exception error) {
                BCError bcError = new(error);
                await this.NextConsumer.OnError(bcError, cancellationToken);
                bcError.ThrowIfNotHandled();
            }
        }
    }

    public override async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnComplete))) {
            if (this.SetCompleting()) {
                this._Channel.Writer.Complete(default);
                if (this.SetCompleted()) {
                    await this._ChannelTrackingNext.OnComplete(cancellationToken);
                }
            }
        }
    }

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            await this._ChannelTrackingNext.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Hint
    // SetMonitor(BCMonitor monitor) 
    // BCChannelTrackingNext is NextConsumer

    private sealed class BCChannelTrackingNext
        : IBCConsumer<TOut>
        , IBCTrackingManager<TIn, TOut> {
        private readonly ConcurrentDictionary<long, BCTracking<TIn, TOut>> _Tracking = new();
        private readonly IBCConsumer<TOut> _Next;
        private readonly TaskCompletionSource _Completion = new();
        private BCLifeTime _LifeTime;

        public BCChannelTrackingNext(
            BCDescription description,
            IBCConsumer<TOut> next
        ) {
            this.Description = description;
            this._Next = next;
        }

        public BCLifeTime LifeTime => this._LifeTime;

        public BCDescription Description { get; }

        public void AddTracking(BCTracking<TIn, TOut> tracking) {
            _ = this._Tracking.TryAdd(tracking.Id, tracking);
        }

        public async Task OnTrackingComplete(BCTracking<TIn, TOut> tracking, CancellationToken cancellationToken) {
            this._Tracking.TryRemove(tracking.Id, out _);
            if (BCLifeTime.Completing == this._LifeTime) {
                if (this._Tracking.IsEmpty) {
                    if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                        this._Completion.SetResult();

                        await this._Next.OnComplete(cancellationToken);
                    }
                }
            }
        }

        public async Task OnTrackingError(BCTracking<TIn, TOut> tracking, BCError value, CancellationToken cancellationToken) {
            await this.OnError(value, cancellationToken);
        }

        public async Task OnComplete(CancellationToken cancellationToken) {
            BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
            if (this._Tracking.IsEmpty) {
                if (BCLifeTimeExtension.SetCompleted(ref this._LifeTime)) {
                    this._Completion.SetResult();

                    await this._Next.OnComplete(cancellationToken);
                }
            }
        }

        public Task OnError(BCError value, CancellationToken cancellationToken) {
            return this._Next.OnError(value, cancellationToken);
        }

        public Task OnNext(TOut value, CancellationToken cancellationToken) {
            return this._Next.OnNext(value, cancellationToken);
        }

        public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
            await this._Completion.Task;
            await this._Next.WaitCompletedAsync(cancellationToken);

        }
    }
}
#endif