namespace Brimborium.Channels;

public class BCDelegateTests{
    [Test]
    public async Task BCDelegate_Test002() {
        CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;
        BCConsumerListValue<int> sink = new(new("Sink"));
        BCBlockI1O1<int, int> block = BCBlockI1O1<int, int>.Create(
            new(new("block range")),
            (out1) => new BCDelegate<int, int>(
                description: new("time i"),
                onNext: async (value, next, cancellationToken) => {
                    await next.OnNextEnumerable(System.Linq.Enumerable.Range(0, value), cancellationToken);
                },
                onError: default,
                onComplete: default,
                next: out1));
        await block.OutgoingProducer1.Subscribe(sink, cancellationToken);
        BCSource<int> source = new(new("source"), block.IncomingConsumer1);
        BCMonitorConsole monitor = new ();
        monitor.AddMonitored(source);
        await Assert.That(((IBCMonitored)sink).GetMonitor()).IsSameReferenceAs(monitor);


        List<int> listInput = [1, 2, 4, 8];
        List<int> listExpected = [0, 0, 1, 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7];
        await source.OnNextEnumerable(listInput, cancellationToken);
        await source.OnComplete(cancellationToken);
        await Assert.That(sink.LifeTime).IsEqualTo(BCLifeTime.Completed);
        var actual = await sink.GetResultAsync(cancellationToken);
        await Assert.That(actual).IsEquivalentTo(listExpected);
    }


    [Test]
    public async Task BCDelegate_Throws_Test003() {
        CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;
        BCConsumerListValue<int> sink = new(new("Sink"));
        BCBlockI1O1<int, int> block = BCBlockI1O1<int, int>.Create(
            new(new("block range")),
            (out1) => new BCDelegate<int, int>(
                description: new("time i"),
                onNext: async (value, next, cancellationToken) => {
                    throw new DivideByZeroException("");
                },
                onComplete: default,
                onError: default,
                next: out1));
        await block.OutgoingProducer1.Subscribe(sink, cancellationToken);
        BCSource<int> source = new(new("source"), block.IncomingConsumer1);
        BCMonitorConsole monitor = new ();
        monitor.AddMonitored(source);
        await Assert.That(((IBCMonitored)sink).GetMonitor()).IsSameReferenceAs(monitor);


        List<int> listInput = [1, 2, 4, 8];
        List<int> listExpected = [1, 2, 4, 8];
        await source.OnNextEnumerable(listInput, cancellationToken);
        await source.OnComplete(cancellationToken);
        await Assert.ThrowsAsync<DivideByZeroException>(() => sink.GetResultAsync(cancellationToken));

    }
}