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

    [Test]
    public async Task BCSource_OnError_PropagatesError() {
        var ct = CancellationToken.None;
        BCConsumerListValue<int> sink = new(new("Sink"));
        BCSource<int> source = new(new("source"), sink);

        var error = new InvalidOperationException("pipeline error");
        await source.OnError(new BCError(error), ct);
        await source.OnComplete(ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sink.GetResultAsync(ct));
    }
}
