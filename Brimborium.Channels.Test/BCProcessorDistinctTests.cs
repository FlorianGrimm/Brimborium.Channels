namespace Brimborium.Channels.Test;

public class BCProcessorDistinctO1Tests {
    [Test]
    public async Task DistinctO1_FirstOccurrence_TaggedFirst() {
        var ct = CancellationToken.None;
        BCConsumerListValue<BCDistinctValue<int>> sink = new(new("sink"));
        BCProcessorDistinctO1<int, int> sut = new(
            new("distinct"),
            v => v,
            EqualityComparer<int>.Default,
            sink);
        BCSource<int> source = new(new("source"), sut);

        await source.OnNextEnumerable([1, 2, 3], ct);
        await source.OnComplete(ct);

        var actual = await sink.GetResultAsync(ct);

        await Assert.That(actual).Count().IsEqualTo(3);
        await Assert.That(actual[0]).IsEqualTo(new BCDistinctValue<int>(1, true));
        await Assert.That(actual[1]).IsEqualTo(new BCDistinctValue<int>(2, true));
        await Assert.That(actual[2]).IsEqualTo(new BCDistinctValue<int>(3, true));
    }

    [Test]
    public async Task DistinctO1_Duplicates_TaggedNotFirst() {
        var ct = CancellationToken.None;
        BCConsumerListValue<BCDistinctValue<int>> sink = new(new("sink"));
        BCProcessorDistinctO1<int, int> sut = new(
            new("distinct"),
            v => v,
            EqualityComparer<int>.Default,
            sink);
        BCSource<int> source = new(new("source"), sut);

        await source.OnNextEnumerable([1, 2, 1, 3, 2], ct);
        await source.OnComplete(ct);

        var actual = await sink.GetResultAsync(ct);

        await Assert.That(actual).Count().IsEqualTo(5);
        await Assert.That(actual[0]).IsEqualTo(new BCDistinctValue<int>(1, true));
        await Assert.That(actual[1]).IsEqualTo(new BCDistinctValue<int>(2, true));
        await Assert.That(actual[2]).IsEqualTo(new BCDistinctValue<int>(1, false));
        await Assert.That(actual[3]).IsEqualTo(new BCDistinctValue<int>(3, true));
        await Assert.That(actual[4]).IsEqualTo(new BCDistinctValue<int>(2, false));
    }

    [Test]
    public async Task DistinctO1_ByKeySelector_UsesKeyForComparison() {
        var ct = CancellationToken.None;
        BCConsumerListValue<BCDistinctValue<string>> sink = new(new("sink"));
        // Key = first character, so "apple" and "avocado" share key 'a'
        BCProcessorDistinctO1<string, char> sut = new(
            new("distinct"),
            s => s[0],
            EqualityComparer<char>.Default,
            sink);
        BCSource<string> source = new(new("source"), sut);

        await source.OnNextEnumerable(["apple", "banana", "avocado"], ct);
        await source.OnComplete(ct);

        var actual = await sink.GetResultAsync(ct);

        await Assert.That(actual).Count().IsEqualTo(3);
        await Assert.That(actual[0].First).IsTrue();   // "apple"  - 'a' first seen
        await Assert.That(actual[1].First).IsTrue();   // "banana" - 'b' first seen
        await Assert.That(actual[2].First).IsFalse();  // "avocado" - 'a' duplicate
    }
}

public class BCProcessorDistinctO2Tests {
    [Test]
    public async Task DistinctO2_FirstSeenGoesToConsumer1_DuplicatesGoToConsumer2() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sinkFirst = new(new("first"));
        BCConsumerListValue<int> sinkDup = new(new("duplicates"));
        BCProcessorDistinctO2<int, int> sut = new(
            new("distinct"),
            v => v,
            EqualityComparer<int>.Default,
            sinkFirst,
            sinkDup);
        BCSource<int> source = new(new("source"), sut);
        BCMonitor monitor = new();
        monitor.AddMonitored(source);

        await source.OnNextEnumerable([1, 2, 1, 3, 2], ct);
        await source.OnComplete(ct);

        var actualFirst = await sinkFirst.GetResultAsync(ct);
        var actualDup = await sinkDup.GetResultAsync(ct);

        await Assert.That(actualFirst).IsEquivalentTo(new List<int> { 1, 2, 3 });
        await Assert.That(actualDup).IsEquivalentTo(new List<int> { 1, 2 });
    }

    [Test]
    public async Task DistinctO2_AllUnique_AllGoToConsumer1() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sinkFirst = new(new("first"));
        BCConsumerListValue<int> sinkDup = new(new("duplicates"));
        BCProcessorDistinctO2<int, int> sut = new(
            new("distinct"),
            v => v,
            EqualityComparer<int>.Default,
            sinkFirst,
            sinkDup);
        BCSource<int> source = new(new("source"), sut);
        BCMonitor monitor = new();
        monitor.AddMonitored(source);

        await source.OnNextEnumerable([10, 20, 30], ct);
        await source.OnComplete(ct);

        var actualFirst = await sinkFirst.GetResultAsync(ct);
        var actualDup = await sinkDup.GetResultAsync(ct);

        await Assert.That(actualFirst).IsEquivalentTo(new List<int> { 10, 20, 30 });
        await Assert.That(actualDup).Count().IsEqualTo(0);
    }
}

