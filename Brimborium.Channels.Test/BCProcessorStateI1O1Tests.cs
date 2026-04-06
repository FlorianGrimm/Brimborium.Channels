namespace Brimborium.Channels.Test;

public class BCProcessorStateI1O1Tests {
    public class SumState {
        public int Value = 0;

        public void Add(int value) {
            this.Value += value;
        }
    }

    [Test]
    public async Task Sum() {
        CancellationTokenSource cts = new CancellationTokenSource();
        BCConsumerSingleValue<int> sink = new("sink");
        var sum = new BCProcessorStateI1O1<SumState, int, int>(
                description: "sum",
                state: new(),
                onNext: (value, state, consumer1, cancellationToken) => {
                    state.Add(value);
                    return Task.CompletedTask;
                },
                onComplete: async (state, consumer1, cancellationToken) => {
                    await consumer1.OnNext(state.Value, cancellationToken);
                    await consumer1.OnComplete(cancellationToken);
                },
                onError: default,
                nextConsumer: sink
            );
        BCSource<int> source = new("source", sum);

        await source.OnNextEnumerable([1, 2, 3, 4, 5], cts.Token);
        await source.OnComplete(cts.Token);

        var act = await sink.GetResultAsync();
        await Assert.That(act).IsEqualTo(1 + 2 + 3 + 4 + 5);
    }
}
