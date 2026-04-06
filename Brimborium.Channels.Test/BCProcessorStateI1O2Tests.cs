namespace Brimborium.Channels.Test;

public class BCProcessorStateI1O2Tests {
    public sealed class SumAvgState {
        public int Value = 0;
        public int Count = 0;

        public void Add(int value) {
            this.Value += value;
            this.Count++;
        }

        public int GetSum()
            => this.Value;

        public double GetAverage() {
            if (this.Count == 0) {
                return 0.0d;
            } else {
                return ((double)this.Value) / ((double)this.Count);
            }
        }
    }

    [Test]
    public async Task SumAvg() {
        CancellationTokenSource cts = new CancellationTokenSource();
        BCConsumerSingleValue<int> sinkSum = new("sinkSum");
        BCConsumerSingleValue<double> sinkAvg = new("sinkAvg");
        var sum = new BCProcessorStateI1O2<SumAvgState, int, int, double>(
                description: "SumAvg",
                state: new(),
                onNext: (value, state, consumer1, consumer2, cancellationToken) => {
                    state.Add(value);
                    return Task.CompletedTask;
                },
                onComplete: async (state, consumer1, consumer2, cancellationToken) => {
                    await consumer1.OnNext(state.GetSum(), cancellationToken);
                    await consumer1.OnComplete(cancellationToken);

                    await consumer2.OnNext(state.GetAverage(), cancellationToken);
                    await consumer2.OnComplete(cancellationToken);
                },
                onError: default,
                nextConsumer1: sinkSum,
                nextConsumer2: sinkAvg
            );
        BCSource<int> source = new("source", sum);

        await source.OnNextEnumerable([1, 2, 3, 4, 5], cts.Token);
        await source.OnComplete(cts.Token);

        var actSum = await sinkSum.GetResultAsync();
        var actAvg = await sinkAvg.GetResultAsync();
        await Assert.That(actSum).IsEqualTo(15);
        await Assert.That(actAvg).IsEqualTo(3.0d);
    }
}
