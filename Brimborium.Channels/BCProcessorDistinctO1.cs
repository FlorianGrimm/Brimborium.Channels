#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

/// <summary>
/// Wraps a value together with a flag indicating whether it is the first occurrence of its key in the stream.
/// </summary>
/// <typeparam name="T">The type of the wrapped value.</typeparam>
public record struct BCDistinctValue<T>(T Value, bool First);

/// <summary>
/// Processor that tags each incoming value as first-seen (<c>First = true</c>) or a duplicate (<c>First = false</c>)
/// by tracking seen keys via a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// Both kinds are forwarded to a single downstream consumer as <see cref="BCDistinctValue{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The type of values received from upstream.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify duplicates.</typeparam>
public sealed class BCProcessorDistinctO1<TValue, TKey>
    : BCProcessorSynced<TValue, BCDistinctValue<TValue>>
    , IBCMonitored
    where TKey : notnull {
    private readonly Func<TValue, TKey> _GetKey;
    private readonly ConcurrentDictionary<TKey, TKey> _SeenBefore;
    
    public BCProcessorDistinctO1(
        BCDescription description,
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

