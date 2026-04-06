namespace Brimborium.Channels.Test;

public class BCBlockChainTests {
    [Test]
    public async Task BCBlockChainTest001() {
        CancellationTokenSource cts = new CancellationTokenSource();
        BCConsumerListValue<int> sinkInt = new(new("sinkInt"));
        BCConsumerListValue<string> sinkText = new(new("sinkText"));
        var blockSplit = BCBlockI1O2<string, int, string>.Create(
            description: new(
                Description: new BCDescription("split"),
                In1: new BCDescription("In1"),
                Out1: new BCDescription("Out1-IsNumber"),
                Out2: new BCDescription("Out2-Fallback")
                ),
            (out1, out2) => {
                var op = new BCDelegateI1O2<string, int, string>(
                    description: new BCDescription("op"),
                    onNext: async (value, out1, out2, cancellationToken) => {
                        if (int.TryParse(value, out int result)) {
                            await out1.OnNext(result, cancellationToken);
                        } else {
                            await out2.OnNext(value, cancellationToken);
                        }
                    },
                    onError: default,
                    onComplete: default,
                    out1,
                    out2
                    );
                return op;
            });
        await blockSplit.OutgoingProducer1.Subscribe(sinkInt, cts.Token);
        await blockSplit.OutgoingProducer2.Subscribe(sinkText, cts.Token);
        BCSource<string> source = new(
            description: new BCDescription("source"),
            blockSplit.IncomingConsumer1);

        await source.OnNext("1",cts.Token);
        await source.OnNext("A",cts.Token);
        await source.OnNext("2",cts.Token);
        await source.OnNext("B",cts.Token);
        await source.OnNext("3",cts.Token);
        await source.OnNext("C", cts.Token);
        await source.OnComplete(cts.Token);

        var actInt = await sinkInt.GetResultAsync(cts.Token);
        var actText = await sinkText.GetResultAsync(cts.Token);

        await Assert.That(actInt).IsEquivalentTo([1, 2, 3]);
        await Assert.That(actText).IsEquivalentTo(["A", "B", "C"]);
    }
}
