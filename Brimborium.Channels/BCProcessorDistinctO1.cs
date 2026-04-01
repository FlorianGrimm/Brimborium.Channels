#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

public record struct BCDistinctValue<T>(T Value, bool First);
public sealed class BCProcessorDistinctO1<TValue, TKey>
    : BCProcessorSynced<TValue, BCDistinctValue<TValue>>
    , IBCMonitored
    where TKey : notnull {
    private readonly Func<TValue, TKey> _GetKey;
    private readonly ConcurrentDictionary<TKey, TKey> _SeenBefore;
    
    public BCProcessorDistinctO1(
        BCDescription? description,
        Func<TValue, TKey> getKey,
        IEqualityComparer<TKey> keyEqualityComparer,
        IBCConsumer<BCDistinctValue<TValue>> next
    ) : base(description, next) {
        this._GetKey = getKey;
        this._SeenBefore = new(keyEqualityComparer);
    }

    public override async Task OnNext(TValue value, CancellationToken cancellationToken) {
        var key = this._GetKey(value);
        if (this._SeenBefore.TryAdd(key, key)) {
            await this.NextConsumer.OnNext(new BCDistinctValue<TValue>(value, true), cancellationToken);
        } else {
            await this.NextConsumer.OnNext(new BCDistinctValue<TValue>(value, false), cancellationToken);
        }
    }
}

