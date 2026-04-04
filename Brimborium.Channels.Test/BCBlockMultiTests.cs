namespace Brimborium.Channels.Test;

public class BCBlockI1O2Tests {
    /// <summary>
    /// BCBlockI1O2: one input, two outputs.
    /// Uses BCProcessorDistinctO2 inside the block to route
    /// first-seen values to OutgoingProducer1 and duplicates to OutgoingProducer2.
    /// </summary>
    [Test]
    public async Task BCBlockI1O2_RoutesByDistinct_FirstAndDuplicatesToSeparateSinks() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sinkFirst = new(new("first"));
        BCConsumerListValue<int> sinkDup = new(new("duplicates"));

        BCBlockI1O2<int, int, int> block = BCBlockI1O2<int, int, int>.Create(
            null,
            (out1, out2) => new BCProcessorDistinctO2<int, int>(
                new BCDescription("1"),
                v => v,
                EqualityComparer<int>.Default,
                out1,
                out2));

        await block.OutgoingProducer1.Subscribe(sinkFirst, ct);
        await block.OutgoingProducer2.Subscribe(sinkDup, ct);

        BCSource<int> source = new(new("source"), block.IncomingConsumer1);
        BCMonitorConsole monitor = new();
        monitor.AddMonitored(source);

        await source.OnNextEnumerable([1, 2, 1, 3, 2], ct);
        await source.OnComplete(ct);

        var actualFirst = await sinkFirst.GetResultAsync(ct);
        var actualDup = await sinkDup.GetResultAsync(ct);

        await Assert.That(actualFirst).IsEquivalentTo(new List<int> { 1, 2, 3 });
        await Assert.That(actualDup).IsEquivalentTo(new List<int> { 1, 2 });
    }

    [Test]
    public async Task BCBlockI1O2_BothOutputsComplete_AfterSourceCompletes() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sinkFirst = new(new("first"));
        BCConsumerListValue<int> sinkDup = new(new("duplicates"));

        BCBlockI1O2<int, int, int> block = BCBlockI1O2<int, int, int>.Create(
            null,
            (out1, out2) => new BCProcessorDistinctO2<int, int>(
                new BCDescription("1"),
                v => v,
                EqualityComparer<int>.Default,
                out1,
                out2));

        await block.OutgoingProducer1.Subscribe(sinkFirst, ct);
        await block.OutgoingProducer2.Subscribe(sinkDup, ct);

        BCSource<int> source = new(new("source"), block.IncomingConsumer1);
        BCMonitorConsole monitor = new();
        monitor.AddMonitored(source);

        await source.OnNextEnumerable([5, 5], ct);
        await source.OnComplete(ct);

        await Assert.That(sinkFirst.LifeTime).IsEqualTo(BCLifeTime.Completed);
        await Assert.That(sinkDup.LifeTime).IsEqualTo(BCLifeTime.Completed);
    }
}

public class BCBlockI2O1Tests {
    /// <summary>
    /// BCBlockI2O1: two inputs, one output.
    /// Source1 passes values as-is; Source2 multiplies values by 10.
    /// All values from both sources arrive at the single sink.
    /// Data from both sources must be sent BEFORE either source completes,
    /// because completing source1 triggers downstream completion.
    /// </summary>
    [Test]
    public async Task BCBlockI2O1_MergesTwoSources_IntoOneSink() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sink = new(new("sink"));

        BCBlockI2O1<int, int, int> block = BCBlockI2O1<int, int, int>.Create(
            null,
            (out1) => new BCDelegate<int, int>(
                new("passthrough"),
                async (v, next, ct) => await next.OnNext(v, ct),
                onError: null,
                onComplete: null,
                next: out1),
            (out1) => new BCDelegate<int, int>(
                new("times-ten"),
                async (v, next, ct) => await next.OnNext(v * 10, ct),
                onError: null,
                onComplete: null,
                next: out1));

        await block.OutgoingProducer1.Subscribe(sink, ct);

        BCSource<int> source1 = new(new("source1"), block.IncomingConsumer1);
        BCSource<int> source2 = new(new("source2"), block.IncomingConsumer2);

        // Send all data first, then complete — prevents premature downstream completion
        await source1.OnNextEnumerable([1, 2, 3], ct);
        await source2.OnNextEnumerable([1, 2, 3], ct);
        await source1.OnComplete(ct);
        await source2.OnComplete(ct);

        var actual = await sink.GetResultAsync(ct);

        await Assert.That(actual).IsEquivalentTo(new List<int> { 1, 2, 3, 10, 20, 30 });
    }

    [Test]
    public async Task BCBlockI2O1_SinkCompletes_AfterFirstSourceCompletes() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sink = new(new("sink"));

        BCBlockI2O1<int, int, int> block = BCBlockI2O1<int, int, int>.Create(
            null,
            (out1) => new BCDelegate<int, int>(
                new BCDescription("1"),
                async (v, next, ct) => await next.OnNext(v, ct),
                null, null, out1),
            (out1) => new BCDelegate<int, int>(
                new BCDescription("2"),
                async (v, next, ct) => await next.OnNext(v, ct),
                null, null, out1));

        await block.OutgoingProducer1.Subscribe(sink, ct);

        BCSource<int> source1 = new(new("source1"), block.IncomingConsumer1);
        BCSource<int> source2 = new(new("source2"), block.IncomingConsumer2);

        await source1.OnNext(42, ct);
        await source1.OnComplete(ct);  // triggers sink OnComplete (no connections tracking)

        await Assert.That(sink.LifeTime).IsEqualTo(BCLifeTime.Completed);

        // source2 never fired; its OnComplete is a no-op (outgoing producer already completed)
        await source2.OnComplete(ct);
    }
}

