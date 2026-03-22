using System.Diagnostics;

namespace Brimborium.Channels.Test;

public class OwningChannelTests
{
    [Test]
    public async Task OwningChannelTest001()
    {
        CancellationTokenSource cts = new();
        var cancellationToken = cts.Token;
        var resultConsumer = await Channel.CreateUnbounded<int>()
            .AsOwningChannel("Simple")
            .InvokeProducerVoid(
                asyncProducer: (channelWriter, cancellationToken) => SimpleProducer(channelWriter, cancellationToken),
                cancellationToken: cancellationToken
                )
            .InvokeConsumer(
                asyncConsumer: (channelReader, cancellationToken) => SimpleConsumer(channelReader, cancellationToken),
                cancellationToken: cancellationToken
            ).RunAsync(continueOnCapturedContext: true)
            ;
        await Assert.That(resultConsumer.Count).IsEqualTo(1000);
    }

    private static async Task SimpleProducer(
        SharedChannelWriter<int> channelWriter,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < 1000; i++)
        {
            if (Random.Shared.Next(100) < 10)
            {
                await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
            }
            await channelWriter.WriteAsync(i, cancellationToken);
        }
    }

    private static async Task<List<int>> SimpleConsumer(
        OwnedChannelReader<int> channelReader,
        CancellationToken cancellationToken)
    {
        List<int> result = new(1000);
        await foreach (var value in channelReader.ReadAllAsync(cancellationToken))
        {
            if (Random.Shared.Next(100) < 10)
            {
                await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
            }
            result.Add(value);
        }
        return result;
    }

    [Test]
    public async Task CombinedWorkTest(CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = new();
        SillyCombinedBulkProcess combinedBulkProcess = new();
        var taskExecute = combinedBulkProcess.ExecuteAsync(cts.Token);
        var listCombinedWorker = System.Linq.Enumerable.Range(0, 100)
            .Select((index) => new CombinedWorker(index))
            .ToList();
        List<Task> listTask = new(100);
        foreach (var combinedWorker in listCombinedWorker)
        {
            var task = combinedWorker.RunAsync(cts.Token);
            listTask.Add(task);
        }

        await Task.CompletedTask;
    }

    public record struct PayloadContinueWith(int Payload, ICallback<int> continueWith);
    public class SillyCombinedBulkProcess : CombinedBulkProcess<PayloadContinueWith>
    {
        public async Task Enqueue(int request, ICallback<int> callback, CancellationToken cancellationToken)
        {
            await this.Enqueue(new(request, callback), cancellationToken);
        }

        protected override async Task ProcessAsync(List<PayloadContinueWith> listInput, CancellationToken cancellationToken)
        {
            foreach (var (value, callback) in listInput)
            {
                await callback.ExecuteAsync(value + 1, cancellationToken);
            }
        }
    }

    class CombinedWorker
    {
        private readonly int _Index;

        public CombinedWorker(int index)
        {
            this._Index = index;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var (resultProducer, resultConsumer) = await Channel.CreateUnbounded<int>()
                .AsOwningChannel($"CombinedWork-{this._Index}")
                .InvokeProducerWithResult(
                    asyncProducer: (channelWriter, cancellationToken) => Producer(channelWriter, cancellationToken),
                    cancellationToken: cancellationToken
                )
                .InvokeConsumer(
                    asyncConsumer: (channelReader, cancellationToken) => Consumer(channelReader, cancellationToken),
                    cancellationToken: cancellationToken
                )
                .RunAsync(true)
                ;
        }

        private static async Task<TimeSpan> Producer(
            SharedChannelWriter<int> channelWriter,
            CancellationToken cancellationToken)
        {
            var tsStart = Stopwatch.GetTimestamp();
            for (int i = 0; i < 1000; i++)
            {
                if (Random.Shared.Next(100) < 10)
                {
                    await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
                }
                await channelWriter.WriteAsync(i, cancellationToken);
            }
            return Stopwatch.GetElapsedTime(tsStart);
        }

        private static async Task<List<int>> Consumer(
            OwnedChannelReader<int> channelReader,
            CancellationToken cancellationToken)
        {
            List<int> result = new(1000);
            await foreach (var value in channelReader.ReadAllAsync(cancellationToken))
            {
                if (Random.Shared.Next(100) < 10)
                {
                    await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
                }
                result.Add(value);
            }
            return result;
        }
    }
}
