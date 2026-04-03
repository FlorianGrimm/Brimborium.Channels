namespace Brimborium.Channels.Test;

public class BCBufferTests {
    [Test]
    public async Task BCBufferTest001() {
        CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;
        BCMonitor monitor = new();

        BCConsumerListValue<string> sink = new(
            new("sink"));
        BCBuffer<int, string> sut = new(
            description: new("sut"),
            onNext: async (value, next, cts) => {
                await next.OnNext(value.ToString(), cts);
            },
            onError: default,
            onComplete: default,
            channel: null,
            next: sink);
        BCSource<int> source = new(new("source"), sut);
        monitor.Add(source);

        int cnt = 10_000;
        for (int i = 1; i <= cnt; i++) {
            await source.OnNext(i, cancellationToken);
        }
        await source.OnComplete(cancellationToken);
        await source.WaitSelfCompletedAsync(cancellationToken);
        await source.WaitRightCompletedAsync(cancellationToken);
        var actual = await sink.GetResultAsync(cancellationToken);
        await Assert.That(actual).Count().IsEqualTo(cnt);
    }

    [Test]
    public async Task BCBuffer_CustomOnError_IsInvokedAndHandled() {
        var ct = CancellationToken.None;
        BCConsumerListValue<string> sink = new(new("sink"));
        bool errorHandled = false;
        BCBuffer<int, string> sut = new(
            description: new("sut"),
            onNext: async (value, next, ct) => await next.OnNext(value.ToString(), ct),
            onError: async (error, next, ct) => {
                errorHandled = true;
                error.SetIsHandled();
                await Task.CompletedTask;
            },
            onComplete: default,
            channel: null,
            next: sink);
        BCSource<int> source = new(new("source"), sut);

        await source.OnError(new BCError(new Exception("oops")), ct);
        await source.OnComplete(ct);
        await source.WaitSelfCompletedAsync(ct);
        await source.WaitRightCompletedAsync(ct);

        await Assert.That(errorHandled).IsTrue();
        var actual = await sink.GetResultAsync(ct);
        await Assert.That(actual).Count().IsEqualTo(0);
    }

    [Test]
    public async Task BCBuffer_CustomOnComplete_IsInvoked() {
        BCMonitor monitor = new();
        var ct = CancellationToken.None;
        BCConsumerListValue<string> sink = new(new("sink"));
        bool completeCalled = false;
        BCBuffer<int, string> sut = new(
            description: new("sut"),
            onNext: async (value, next, ct) => await next.OnNext(value.ToString(), ct),
            onError: default,
            onComplete: async (next, ct) => {
                completeCalled = true;
                await next.OnComplete(ct);
            },
            channel: null,
            next: sink);
        BCSource<int> source = new(new("source"), sut);
        source.SetMonitor(monitor);

        await source.OnNext(1, ct);
        await source.OnNext(2, ct);
        await source.OnComplete(ct);
        await source.WaitSelfCompletedAsync(ct);
        await source.WaitRightCompletedAsync(ct);

        await Assert.That(completeCalled).IsTrue();
        var actual = await sink.GetResultAsync(ct);
        await Assert.That(actual).IsEquivalentTo(new List<string> { "1", "2" });
    }
}
