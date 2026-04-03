#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

public sealed class BCProcessorDistinctO2<TValue, TKey>
    : BCProcessorSyncedO2<TValue, TValue, TValue>
    where TKey : notnull {
    private readonly ConcurrentDictionary<TKey, TKey> _SeenBefore;
    private readonly Func<TValue, TKey> _GetKey;

    public BCProcessorDistinctO2(
            BCDescription description,
            Func<TValue, TKey> getKey,
            IEqualityComparer<TKey> keyEqualityComparer,
            IBCConsumer<TValue> nextConsumer1,
            IBCConsumer<TValue> nextConsumer2
        ) : base(
            description,
            nextConsumer1,
            nextConsumer2
        ) {
        this._GetKey = getKey;
        this._SeenBefore = new(keyEqualityComparer);
    }

    public override async Task OnNext(TValue value, CancellationToken cancellationToken) {
        using (this._Monitor?.LogEnter(this, nameof(this.OnNext))) {
            await this._Semaphore.WaitAsync(cancellationToken);
            try {
                var key = this._GetKey(value);
                if (this._SeenBefore.TryAdd(key, key)) {
                    await this.NextConsumer1.OnNext(value, cancellationToken);
                } else {
                    await this.NextConsumer2.OnNext(value, cancellationToken);
                }
            } finally {
                this._Semaphore.Release();
            }
        }
    }
}

