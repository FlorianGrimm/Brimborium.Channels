namespace Brimborium.Channels.Test;

public class BCConsumerSingleValueTests {
    [Test]
    public async Task BCConsumerSingleValueTest001() {

        BCConsumerSingleValue<string> sut = new(new("sut"));
        await sut.OnNext("abc", CancellationToken.None);
        await sut.OnComplete(CancellationToken.None);
        var actual = await sut.GetResultAsync();
        await Assert.That(actual).IsEqualTo("abc");
    }

    [Test]
    public async Task BCConsumerSingleValue_OnError_ThrowsOnGetResult() {
        var ct = CancellationToken.None;
        BCConsumerSingleValue<string> sut = new(new("sut"));
        var error = new InvalidOperationException("pipeline error");
        await sut.OnError(new BCError(error), ct);
        await sut.OnComplete(ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetResultAsync());
    }

    [Test]
    public async Task BCConsumerSingleValue_NoValue_CancelledOnGetResult() {
        var ct = CancellationToken.None;
        BCConsumerSingleValue<string> sut = new(new("sut"));
        await sut.OnComplete(ct);
        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetResultAsync());
    }
}
