#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Brimborium.Channels;

public sealed class BCChannelTracking<TIn, TOut>
    : BCProcessorUnsync<TIn, TOut>
    , IBCMonitored {
    private readonly Channel<BCTracking<TIn, TOut>> _Channel;
    private readonly BCChannelTrackingNext _ChannelTrackingNext;
    private BCMonitor? _Monitor;

    public BCChannelTracking(
            BCDescription? description,
            Channel<BCTracking<TIn, TOut>>? channel,
            IBCConsumer<TOut> next
        ) : this(
            description: description,
            channel: channel,
            channelTrackingNext: new(
                next)
        ) {
    }

    private BCChannelTracking(
            BCDescription? description,
            Channel<BCTracking<TIn, TOut>>? channel,
            BCChannelTrackingNext channelTrackingNext
        ) : base(
            description,
            channelTrackingNext
        ) {
        this._Channel = channel ?? System.Threading.Channels.Channel.CreateUnbounded<BCTracking<TIn, TOut>>();
        this._ChannelTrackingNext = channelTrackingNext;
    }

    public override async Task OnNext(TIn value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnNext")) {
            try {
                var tracking = new BCTracking<TIn, TOut>(value, this._ChannelTrackingNext, this._ChannelTrackingNext);
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
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            if (this.SetCompleting()) {
                this._Channel.Writer.Complete(default);
                if (this.SetCompleted()) {
                    await this._ChannelTrackingNext.OnComplete(cancellationToken);
                }
            }
        }
    }

    public override async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        // no await this._Channel.Reader.Completion;
        using (this._Monitor?.LogEnter(this, "WaitCompletedAsync")) {
            await this._ChannelTrackingNext.WaitCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public class BCChannelTrackingNext
        : IBCConsumer<TOut>
        , IBCTrackingManager<TIn, TOut> {
        private readonly ConcurrentDictionary<long, BCTracking<TIn, TOut>> _Tracking = new();
        private readonly IBCConsumer<TOut> _Next;
        private readonly TaskCompletionSource _Completion = new();
        private BCLifeTime _LifeTime;

        public BCChannelTrackingNext(
            IBCConsumer<TOut> next
        ) {
            this._Next = next;
        }

        public BCLifeTime LifeTime => this._LifeTime;

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

        public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
            await this._Completion.Task;
            await this._Next.WaitCompletedAsync(cancellationToken);

        }
    }
}

public interface IBCTrackingManager<TIn, TOut> {
    Task OnTrackingComplete(BCTracking<TIn, TOut> tracking, CancellationToken cancellationToken);
    Task OnTrackingError(BCTracking<TIn, TOut> tracking, BCError value, CancellationToken cancellationToken);
}

public class BCTracking<TIn, TOut> 
    : IBCConsumer<TOut>
    , IBCMonitored {
    private static long _NextId;
    internal readonly long Id;
    private BCLifeTime _LifeTime;
    protected readonly IBCTrackingManager<TIn, TOut> _TrackingManager;
    private readonly IBCConsumer<TOut> _NextConsumer;

    public BCTracking(
        TIn Value,
        IBCTrackingManager<TIn, TOut> TrackingManager,
        IBCConsumer<TOut> nextConsumer
        ) {
        this.Value = Value;
        this._TrackingManager = TrackingManager;
        this._NextConsumer = nextConsumer;
        this.Id = System.Threading.Interlocked.Increment(ref _NextId);
    }

    public BCLifeTime LifeTime => this._LifeTime;

    public TIn Value { get; }

    public async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            if (BCLifeTimeExtension.SetCompleting(ref this._LifeTime)) {
                BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
                await this._TrackingManager.OnTrackingComplete(this, cancellationToken);
                if (this._Completion is { } completion) {
                    completion.SetResult();
                }
            }
        }
    }

    public async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            await this._TrackingManager.OnTrackingError(this, value, cancellationToken);
        }
    }

    private TaskCompletionSource? _Completion;
    private BCMonitor? _Monitor;

    public Task WaitCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            if (this._Completion is { } completion) {
                return completion.Task;
            }
            if (BCLifeTime.Completed == this._LifeTime) {
                return Task.CompletedTask;
            }

            return (this._Completion = new TaskCompletionSource()).Task;
        }
    }

    public async Task OnNext(TOut value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, "OnComplete")) {
            await this._NextConsumer.OnNext(value, cancellationToken);
        }
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public void SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return; }
        this._Monitor = monitor;
    }
}
