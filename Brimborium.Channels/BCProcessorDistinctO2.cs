#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

/// <summary>
/// Processor that routes each incoming value to one of two downstream consumers based on uniqueness:
/// first-seen values go to <c>NextConsumer1</c>; duplicates go to <c>NextConsumer2</c>.
/// Uniqueness is determined by a caller-supplied key selector and equality comparer.
/// </summary>
/// <typeparam name="TValue">The type of values received from upstream.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify duplicates.</typeparam>
public sealed class BCProcessorDistinctO2<TValue, TKey>
    : BCProcessorSyncedI1O2<TValue, TValue, TValue>
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
            await this._Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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

    public override Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public override Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}

