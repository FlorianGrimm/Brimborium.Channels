namespace Brimborium.Channels;

public abstract class BCProcessorSyncedI2O1<TIn1, TIn2, TOut1>
    : BCPartMonitored
    , IBCConsumer
    , IBCConsumer<TOut1> {
    protected BCConsumer1 _Consumer1;
    protected BCConsumer2 _Consumer2;

    public IBCConsumer<TIn1> Consumer1 => this._Consumer1;

    public IBCConsumer<TIn2> Consumer2 => this._Consumer2;

    protected IBCConsumer<TOut1> NextConsumer1;

    private readonly SemaphoreSlim _SemaphoreNextConsumer = new(1, 1);
    protected readonly SemaphoreSlim _Semaphore = new(1, 1);

    protected BCProcessorSyncedI2O1(
            BCDescription description,
            IBCConsumer<TOut1> nextConsumer1
        ) : base(
            description
        ) {
        this.NextConsumer1 = nextConsumer1;
        this._Consumer1 = new($"{description.Name}-1", this);
        this._Consumer2 = new($"{description.Name}-2", this);
    }

    public abstract Task OnNext1(TIn1 value, CancellationToken cancellationToken);
    public abstract Task OnNext2(TIn2 value, CancellationToken cancellationToken);

    public abstract Task OnError1(BCError value, CancellationToken cancellationToken);
    public abstract Task OnError2(BCError value, CancellationToken cancellationToken);

    public abstract Task OnComplete1(CancellationToken cancellationToken);
    public abstract Task OnComplete2(CancellationToken cancellationToken);

    public virtual async Task OnNext(TOut1 value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(OnNext))) {
            await this._SemaphoreNextConsumer.WaitAsync(cancellationToken);
            try {
                await this.NextConsumer1.OnNext(value, cancellationToken);
            } finally {
                this._SemaphoreNextConsumer.Release();
            }
        }
    }

    public virtual async Task OnError(BCError value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(OnError))) {
            await this._SemaphoreNextConsumer.WaitAsync(cancellationToken);
            try {
                await this.NextConsumer1.OnError(value, cancellationToken);
            } finally {
                this._SemaphoreNextConsumer.Release();
            }
        }
    }

    public virtual async Task OnComplete(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(OnComplete))) {
            await this._SemaphoreNextConsumer.WaitAsync(cancellationToken);
            try {
                this.SetCompleting();
                if (this.SetCompleted()) {
                    await this.NextConsumer1.OnComplete(cancellationToken);
                }
            } finally {
                this._SemaphoreNextConsumer.Release();
            }
        }
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(WaitSelfCompletedAsync))) {
            await this._SemaphoreNextConsumer.WaitAsync(cancellationToken);
            this._SemaphoreNextConsumer.Release();
        }
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(WaitRightCompletedAsync))) {
            await this.NextConsumer1.WaitSelfCompletedAsync(cancellationToken);
            await this.NextConsumer1.WaitRightCompletedAsync(cancellationToken);
        } 
    }

    public sealed class BCConsumer1
        : BCPartMonitored
        , IBCConsumer<TIn1> {
        private readonly BCProcessorSyncedI2O1<TIn1, TIn2, TOut1> _Owner;

        public BCConsumer1(
                BCDescription description,
                BCProcessorSyncedI2O1<TIn1, TIn2, TOut1> owner
            ) : base(
                description
            ) {
            this._Owner = owner;
        }

        public Task OnNext(TIn1 value, CancellationToken cancellationToken) {
            return this._Owner.OnNext1(value, cancellationToken);
        }


        public async Task OnError(BCError value, CancellationToken cancellationToken) {
            await this._Owner.OnError1(value, cancellationToken).ConfigureAwait(false);
        }

        public async Task OnComplete(CancellationToken cancellationToken) {
            this.SetCompleting();
            if (this.SetCompleted()) {
                await this._Owner.OnComplete1(cancellationToken).ConfigureAwait(false);
            }
        }

        public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
            return this._Owner.WaitRightCompletedAsync(cancellationToken);
        }

        public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
            return this._Owner.WaitSelfCompletedAsync(cancellationToken);
        }
    }

    public sealed class BCConsumer2
    : BCPartMonitored
    , IBCConsumer<TIn2> {
        private readonly BCProcessorSyncedI2O1<TIn1, TIn2, TOut1> _Owner;

        public BCConsumer2(
                BCDescription description,
                BCProcessorSyncedI2O1<TIn1, TIn2, TOut1> owner
            ) : base(
                description
            ) {
            this._Owner = owner;
        }

        public Task OnNext(TIn2 value, CancellationToken cancellationToken) {
            return this._Owner.OnNext2(value, cancellationToken);
        }

        public async Task OnError(BCError value, CancellationToken cancellationToken) {
            await this._Owner.OnError2(value, cancellationToken).ConfigureAwait(false);
        }

        public async Task OnComplete(CancellationToken cancellationToken) {
            this.SetCompleting();
            if (this.SetCompleted()) {
                await this._Owner.OnComplete2(cancellationToken).ConfigureAwait(false);
            }
        }

        public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
            return this._Owner.WaitRightCompletedAsync(cancellationToken);
        }

        public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
            return this._Owner.WaitSelfCompletedAsync(cancellationToken);
        }
    }
}
