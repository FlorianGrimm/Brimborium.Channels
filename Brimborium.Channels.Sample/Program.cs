using System.Diagnostics;
using System.Threading.Channels;

namespace Brimborium.Channels.Sample;

internal class Program {
    static async Task Main(string[] args) {
        CancellationTokenSource cts = new();
        await Simple(cts.Token);
        await Simple2(cts.Token);
        await Simple3ProducerReturnsValue(cts.Token);
        await Simple4ProducerReturnsValue(cts.Token);
        await CombinedWork(cts.Token);
    }

    #region Simple

    private static async Task Simple(CancellationToken cancellationToken) {
        var channel = Channel.CreateUnbounded<int>()
            .AsOwningChannel("Simple");
        var channelWriter = channel.GetWriter();
        var taskProducer = SimpleProducer(
                channelWriter.AsShared(),
                cancellationToken)
            .WriterComplete(channelWriter);
        var taskConsumer = SimpleConsumer(
            channel.GetReader(),
            cancellationToken);

        await taskProducer;
        var resultConsumer = await taskConsumer;

        if (1000 == resultConsumer.Count) {
            System.Console.Out.WriteLine("Simple: Ok");
        } else {
            System.Console.Out.WriteLine($"Simple: Error {resultConsumer.Count}");
        }
    }

    private static async Task SimpleProducer(
        SharedChannelWriter<int> channelWriter,
        CancellationToken cancellationToken) {
        for (int i = 0; i < 1000; i++) {
            if (Random.Shared.Next(100) < 10) {
                await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
            }
            await channelWriter.WriteAsync(i, cancellationToken);
        }
    }

    private static async Task<List<int>> SimpleConsumer(
        OwnedChannelReader<int> channelReader,
        CancellationToken cancellationToken) {
        List<int> result = new(1000);
        await foreach (var value in channelReader.ReadAllAsync(cancellationToken)) {
            if (Random.Shared.Next(100) < 10) {
                await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
            }
            result.Add(value);
        }
        return result;
    }

    #endregion Simple

    #region Simple2

    private static async Task Simple2(CancellationToken cancellationToken) {
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

        if (1000 == resultConsumer.Count) {
            System.Console.Out.WriteLine("Simple: Ok");
        } else {
            System.Console.Out.WriteLine($"Simple: Error {resultConsumer.Count}");
        }
    }

    #endregion Simple2


    #region Simple3ProducerReturnsValue

    private static async Task Simple3ProducerReturnsValue(CancellationToken cancellationToken) {
        var channel = Channel.CreateUnbounded<int>()
            .AsOwningChannel("Simple3ProducerReturnsValue");
        var channelWriter = channel.GetWriter();
        var taskProducer = Simple3Producer(
                channelWriter.AsShared(),
                cancellationToken)
            .WriterComplete(channelWriter);
        var taskConsumer = Simple3Consumer(
            channel.GetReader(),
            cancellationToken);

        var resultProducer = await taskProducer;
        var resultConsumer = await taskConsumer;

        if (1000 == resultConsumer.Count) {
            System.Console.Out.WriteLine($"Simple3: Ok {resultProducer} {resultConsumer.Count}");
        } else {
            System.Console.Out.WriteLine($"Simple3: Error {resultProducer} {resultConsumer.Count}");
        }
    }

    private static async Task<TimeSpan> Simple3Producer(
        SharedChannelWriter<int> channelWriter,
        CancellationToken cancellationToken) {
        var tsStart = Stopwatch.GetTimestamp();
        for (int i = 0; i < 1000; i++) {
            if (Random.Shared.Next(100) < 10) {
                await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
            }
            await channelWriter.WriteAsync(i, cancellationToken);
        }
        return Stopwatch.GetElapsedTime(tsStart);
    }

    private static async Task<List<int>> Simple3Consumer(
        OwnedChannelReader<int> channelReader,
        CancellationToken cancellationToken) {
        List<int> result = new(1000);
        await foreach (var value in channelReader.ReadAllAsync(cancellationToken)) {
            if (Random.Shared.Next(100) < 10) {
                await Task.Delay(Random.Shared.Next(50) + 10, cancellationToken);
            }
            result.Add(value);
        }
        return result;
    }

    #endregion Simple3



    #region Simple4ProducerReturnsValue

    private static async Task Simple4ProducerReturnsValue(CancellationToken cancellationToken) {
        var (resultProducer, resultConsumer) = await Channel.CreateUnbounded<int>()
            .AsOwningChannel("Simple4ProducerReturnsValue")
            .InvokeProducerWithResult(
                asyncProducer: (channelWriter, cancellationToken) => Simple3Producer(channelWriter, cancellationToken),
                cancellationToken: cancellationToken
            )
            .InvokeConsumer(
                asyncConsumer: (channelReader, cancellationToken) => Simple3Consumer(channelReader, cancellationToken),
                cancellationToken: cancellationToken
            )
            .RunAsync(true)
            ;



        if (1000 == resultConsumer.Count) {
            System.Console.Out.WriteLine($"Simple3: Ok {resultProducer} {resultConsumer.Count}");
        } else {
            System.Console.Out.WriteLine($"Simple3: Error {resultProducer} {resultConsumer.Count}");
        }
    }


    #endregion Simple4


    #region CombinedWork

    private static async Task CombinedWork(CancellationToken cancellationToken) {
        CancellationTokenSource cts = new();
        SillyCombinedBulkProcess combinedBulkProcess = new();
        var taskExecute = combinedBulkProcess.ExecuteAsync(cts.Token);
        var listCombinedWorker = System.Linq.Enumerable.Range(0, 100)
            .Select((index) => new CombinedWorker(index))
            .ToList();
        List<Task> listTask = new(100);
        foreach (var combinedWorker in listCombinedWorker) {
            var task = combinedWorker.RunAsync(cts.Token);
            listTask.Add(task);
        }

        await Task.CompletedTask;
    }

    public record struct PayloadContinueWith(int Payload, ICallback<int> continueWith);
    public class SillyCombinedBulkProcess : CombinedBulkProcess<PayloadContinueWith> {
        public async Task Enqueue(int request, ICallback<int> callback, CancellationToken cancellationToken) {
            await this.Enqueue(new(request, callback), cancellationToken);
        }

        protected override async Task ProcessAsync(List<PayloadContinueWith> listInput, CancellationToken cancellationToken) {
            foreach (var (value,callback) in listInput) {
                await callback.ExecuteAsync(value + 1, cancellationToken);
            }
        }
    }

    public class CombinedWorker {
        private readonly int _Index;

        public CombinedWorker(int index) {
            this._Index = index;
        }

        public async Task RunAsync(CancellationToken cancellationToken) {
            var (resultProducer, resultConsumer) = await Channel.CreateUnbounded<int>()
                .AsOwningChannel($"CombinedWork-{this._Index}")
                .InvokeProducerWithResult(
                    asyncProducer: (channelWriter, cancellationToken) => Simple3Producer(channelWriter, cancellationToken),
                    cancellationToken: cancellationToken
                )
                .InvokeConsumer(
                    asyncConsumer: (channelReader, cancellationToken) => Simple3Consumer(channelReader, cancellationToken),
                    cancellationToken: cancellationToken
                )
                .RunAsync(true)
                ;
        }
    }
    #endregion CombinedWork
}
