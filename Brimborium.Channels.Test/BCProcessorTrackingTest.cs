using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Brimborium.Channels.Test;

public class BCProcessorTrackingTest {
    [Test]
    public async Task BCProcessorTrackingTest001() {
        CancellationTokenSource cts = new CancellationTokenSource();
        BCMonitor monitor = new BCMonitor();
        BCConsumerListValue<string> sink = new(new("sink"));
        SutProcessorTracking sut = new(new("sut"), new(), sink);
        BCSource<int> source = new(new("source"), sut);
        monitor.Add(source);
        await source.OnNext(10, cts.Token);
        await source.OnNext(30, cts.Token);
        await source.OnComplete(cts.Token);
        var actual = await sink.GetResultAsync(cts.Token);

        await Assert.That(actual).IsEquivalentTo([
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
        ]);
        cts.Cancel();
    }

    public class SutProcessorTracking 
        : BCProcessorTracking<int, string, BCTracking<int, string>> {
        private readonly TestWorker _Worker;

        public SutProcessorTracking(
                BCDescription description,
                TestWorker worker,
                IBCConsumer<string> nextConsumer
            ) : base(
                description, nextConsumer
            ) {
            this._Worker = worker;
        }

        protected override BCTracking<int, string> CreateRequest(
            int Value
            ) {
            return new BCTracking<int, string>(
                description: this.Description,
                Value: Value,
                trackingManager: this.TrackingManager,
                nextConsumer: this.TrackingNext);
        }

        protected override async Task SendRequest(
            BCTracking<int, string> tracking,
            CancellationToken cancellationToken) {
            await this._Worker.Enqueue(tracking, cancellationToken);
        }
    }

    public class TestWorker {
        private readonly Channel<BCTracking<int, string>> _Channel;
        private Task? _TaskRunning;

        public TestWorker() {
            this._Channel = System.Threading.Channels.Channel.CreateUnbounded<BCTracking<int, string>>();
        }
        public async Task Enqueue(BCTracking<int, string> value, CancellationToken cancellationToken) {
            await this._Channel.Writer.WriteAsync(value, cancellationToken);

            if (this._TaskRunning is null) {
                this.Start(cancellationToken);
            }
        }

        private void Start(CancellationToken cancellationToken) {
            if (this._TaskRunning is null) {
                lock (this) {
                    if (this._TaskRunning is null) {
                        this._TaskRunning = this.ExecuteAsync(cancellationToken);
                    }
                }
            }
        }

        private async Task ExecuteAsync(CancellationToken cancellationToken) {
            var reader = this._Channel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken)) {
                while (reader.TryRead(out var tracking)) {
                    foreach (var i in System.Linq.Enumerable.Range(0, tracking.Value)) {
                        await tracking.OnNext(i.ToString(), cancellationToken);
                        await Task.Delay(i).ConfigureAwait(false);
                    }
                    await tracking.OnComplete(cancellationToken);
                }
            }
        }
    }
}
