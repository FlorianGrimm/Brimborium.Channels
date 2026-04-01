namespace Brimborium.Channels;

public class BCSourceTests {
    [Test]
    public async Task BCSource_Direct_Test001() {
        CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;
        BCConsumerListValue<int> sink = new(new("Sink"));
        BCSource<int> source = new(new("source"), sink);
        BCMonitor monitor = new ();
        monitor.AddMonitored(source);
        await Assert.That(((IBCMonitored)sink).GetMonitor()).IsSameReferenceAs(monitor);

        List<int> listInput = [1, 2, 4, 8];
        List<int> listExpected = [1, 2, 4, 8];
        await source.OnNextEnumerable(listInput, cancellationToken);
        await source.OnComplete(cancellationToken);
        var actual = await sink.GetResultAsync(cancellationToken);
        await Assert.That(actual).IsEquivalentTo(listExpected);
    }
}
