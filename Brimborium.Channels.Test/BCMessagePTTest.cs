namespace Brimborium.Channels.Test;

public class BCMessagePTTest {
    // ── OnNext ────────────────────────────────────────────────────────────────

    [Test]
    public async Task OnNext_TryGetOnNext_ReturnsTrue_AndValue() {
        var sut = BCMessage<string, int>.OnNext("One", 1);
        var success = sut.TryGetOnNext(out var value);
        await Assert.That(success).IsTrue();
        await Assert.That(value).IsEqualTo(1);
    }

    [Test]
    public async Task OnNext_TryGetOnError_ReturnsFalse() {
        var sut = BCMessage<string, int>.OnNext("One", 1);
        var success = sut.TryGetOnError(out var error);
        await Assert.That(success).IsFalse();
        await Assert.That(error).IsNull();
    }

    [Test]
    public async Task OnNext_TryGetOnComplete_ReturnsFalse() {
        var sut = BCMessage<string, int>.OnNext("One", 1);
        await Assert.That(sut.TryGetOnComplete()).IsFalse();
    }

    // ── OnError ───────────────────────────────────────────────────────────────

    [Test]
    public async Task OnError_TryGetOnError_ReturnsTrue_AndError() {
        var ex = new InvalidOperationException("boom");
        var bcError = new BCError(ex);
        var sut = BCMessage<string, int>.OnError("Two", bcError);
        var success = sut.TryGetOnError(out var error);
        await Assert.That(success).IsTrue();
        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Error).IsSameReferenceAs(ex);
    }

    [Test]
    public async Task OnError_TryGetOnNext_ReturnsFalse() {
        var sut = BCMessage<string, int>.OnError("Two", new BCError(new Exception("e")));
        var success = sut.TryGetOnNext(out var value);
        await Assert.That(success).IsFalse();
        await Assert.That(value).IsEqualTo(default(int));
    }

    [Test]
    public async Task OnError_TryGetOnComplete_ReturnsFalse() {
        var sut = BCMessage<string, int>.OnError("Two", new BCError(new Exception("e")));
        await Assert.That(sut.TryGetOnComplete()).IsFalse();
    }

    // ── OnComplete ────────────────────────────────────────────────────────────

    [Test]
    public async Task OnComplete_TryGetOnComplete_ReturnsTrue() {
        var sut = BCMessage<string, int>.OnComplete("Three");
        await Assert.That(sut.TryGetOnComplete()).IsTrue();
    }

    [Test]
    public async Task OnComplete_TryGetOnNext_ReturnsFalse() {
        var sut = BCMessage<string, int>.OnComplete("Three");
        var success = sut.TryGetOnNext(out var value);
        await Assert.That(success).IsFalse();
        await Assert.That(value).IsEqualTo(default(int));
    }

    [Test]
    public async Task OnComplete_TryGetOnError_ReturnsFalse() {
        var sut = BCMessage<string, int>.OnComplete("Three");
        var success = sut.TryGetOnError(out var error);
        await Assert.That(success).IsFalse();
        await Assert.That(error).IsNull();
    }

    // ── Parameter is preserved ────────────────────────────────────────────────

    [Test]
    public async Task OnNext_Parameter_IsPreserved() {
        var sut = BCMessage<string, int>.OnNext("myParam", 42);
        await Assert.That(sut.Parameter).IsEqualTo("myParam");
    }

    [Test]
    public async Task OnError_Parameter_IsPreserved() {
        var sut = BCMessage<string, int>.OnError("errParam", new BCError(new Exception()));
        await Assert.That(sut.Parameter).IsEqualTo("errParam");
    }

    [Test]
    public async Task OnComplete_Parameter_IsPreserved() {
        var sut = BCMessage<string, int>.OnComplete("doneParam");
        await Assert.That(sut.Parameter).IsEqualTo("doneParam");
    }
}
