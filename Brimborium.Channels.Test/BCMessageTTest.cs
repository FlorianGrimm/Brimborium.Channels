namespace Brimborium.Channels.Test;

public class BCMessageTTest {
    // ── OnNext ────────────────────────────────────────────────────────────────

    [Test]
    public async Task OnNext_TryGetOnNext_ReturnsTrue_AndValue() {
        var sut = BCMessage<int>.OnNext(1);
        var success = sut.TryGetOnNext(out var value);
        await Assert.That(success).IsTrue();
        await Assert.That(value).IsEqualTo(1);
    }

    [Test]
    public async Task OnNext_TryGetOnError_ReturnsFalse() {
        var sut = BCMessage<int>.OnNext(1);
        var success = sut.TryGetOnError(out var error);
        await Assert.That(success).IsFalse();
        await Assert.That(error).IsNull();
    }

    [Test]
    public async Task OnNext_TryGetOnComplete_ReturnsFalse() {
        var sut = BCMessage<int>.OnNext(1);
        await Assert.That(sut.TryGetOnComplete()).IsFalse();
    }

    // ── OnError ───────────────────────────────────────────────────────────────

    [Test]
    public async Task OnError_TryGetOnError_ReturnsTrue_AndError() {
        var ex = new InvalidOperationException("boom");
        var bcError = new BCError(ex);
        var sut = BCMessage<int>.OnError(bcError);
        var success = sut.TryGetOnError(out var error);
        await Assert.That(success).IsTrue();
        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Error).IsSameReferenceAs(ex);
    }

    [Test]
    public async Task OnError_TryGetOnNext_ReturnsFalse() {
        var sut = BCMessage<int>.OnError(new BCError(new Exception("e")));
        var success = sut.TryGetOnNext(out var value);
        await Assert.That(success).IsFalse();
        await Assert.That(value).IsEqualTo(default(int));
    }

    [Test]
    public async Task OnError_TryGetOnComplete_ReturnsFalse() {
        var sut = BCMessage<int>.OnError(new BCError(new Exception("e")));
        await Assert.That(sut.TryGetOnComplete()).IsFalse();
    }

    // ── OnComplete ────────────────────────────────────────────────────────────

    [Test]
    public async Task OnComplete_TryGetOnComplete_ReturnsTrue() {
        var sut = BCMessage<int>.OnComplete();
        await Assert.That(sut.TryGetOnComplete()).IsTrue();
    }

    [Test]
    public async Task OnComplete_TryGetOnNext_ReturnsFalse() {
        var sut = BCMessage<int>.OnComplete();
        var success = sut.TryGetOnNext(out var value);
        await Assert.That(success).IsFalse();
        await Assert.That(value).IsEqualTo(default(int));
    }

    [Test]
    public async Task OnComplete_TryGetOnError_ReturnsFalse() {
        var sut = BCMessage<int>.OnComplete();
        var success = sut.TryGetOnError(out var error);
        await Assert.That(success).IsFalse();
        await Assert.That(error).IsNull();
    }
}
