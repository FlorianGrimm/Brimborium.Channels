using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Brimborium.Channels.Test;

public class BCProcessorTrackingTest {

    // --- 001 ---

    [Test]
    public async Task BCProcessorTrackingTest001() {
        CancellationTokenSource cts = new CancellationTokenSource();
        BCMonitor monitor = new BCMonitor();
        BCConsumerListValue<string> sink = new(new("sink"));
        BCDelegate<BCMessage<int, string>, string> transform = new(
            description: new BCDescription("transform"),
            onNext: async (message, next, cancellationToken) => {
                if (message.TryGetOnNext(out var value)) {
                    await next.OnNext($"{message.Parameter}-{value}", cancellationToken);
                }
            },
            onError: default,
            onComplete: default,
            next: sink);
        SutProcessorTracking001 sut = new(new("sut"), new(), transform);
        BCSource<int> source = new(new("source"), sut);
        monitor.Add(source);
        await source.OnNext(10, cts.Token);
        await source.OnNext(30, cts.Token);
        await source.OnComplete(cts.Token);
        var actual = await sink.GetResultAsync(cts.Token);

        await Assert.That(actual).IsEquivalentTo([
            "10-0", "10-1", "10-2", "10-3", "10-4", "10-5", "10-6", "10-7", "10-8", "10-9",
            "30-0", "30-1", "30-2", "30-3", "30-4", "30-5", "30-6", "30-7", "30-8", "30-9",
            "30-10", "30-11", "30-12", "30-13", "30-14", "30-15", "30-16", "30-17", "30-18", "30-19",
            "30-20", "30-21", "30-22", "30-23", "30-24", "30-25", "30-26", "30-27", "30-28", "30-29",
        ]);
        cts.Cancel();
    }

    public class SutProcessorTracking001
        : BCProcessorTracking<int, string, BCTracking<int, string>> {
        private readonly TestWorker001 _Worker;
        private readonly BCTrackingConsumer<int, string> _TrackingConsumer;

        public SutProcessorTracking001(
                BCDescription description,
                TestWorker001 worker,
                IBCConsumer<BCMessage<int, string>> nextConsumer
            ) : base(
                description, nextConsumer
            ) {
            this._Worker = worker;
            this._TrackingConsumer = new BCTrackingConsumer<int, string>(
                description,
                this.TrackingManager,
                nextConsumer);
        }

        protected override BCTracking<int, string> CreateRequest(
            int Value
            ) {
            return new BCTracking<int, string>(
                description: this.Description,
                Value: Value,
                nextTrackingConsumer: this.NextTrackingConsumer);
        }

        protected override async Task SendRequest(
            BCTracking<int, string> tracking,
            CancellationToken cancellationToken) {
            await this._Worker.Enqueue(tracking, cancellationToken);
        }
    }

    public class TestWorker001 {
        private readonly Channel<BCTracking<int, string>> _Channel;
        private Task? _TaskRunning;

        public TestWorker001() {
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
                    System.Console.Out.WriteLine($"Workter001:{tracking.Value}:Start");
                    foreach (var i in System.Linq.Enumerable.Range(0, tracking.Value)) {
                        await tracking.OnNext(i.ToString(), cancellationToken);
                        await Task.Delay(((i%10)+1)*10).ConfigureAwait(false);
                    }
                    await tracking.OnComplete(cancellationToken);
                    System.Console.Out.WriteLine($"Workter001:{tracking.Value}:End");
                }
            }
        }
    }




    [Test]
    public async Task BCProcessorTrackingTest002() {
        CancellationTokenSource cts = new CancellationTokenSource();
        BCMonitor monitor = new BCMonitor();
        BCConsumerListValue<string> sink = new(new("sink"));
        BCDelegate<BCMessage<int, string>, string> transform = new(
            description: new BCDescription("transform"),
            onNext: async (message, next, cancellationToken) => {
                if (message.TryGetOnNext(out var value)) {
                    await next.OnNext($"{message.Parameter}-{value}", cancellationToken);
                }
            },
            onError: default,
            onComplete: default,
            next: sink);
        SutProcessorTracking002 sut = new(new("sut"), new(), transform);
        BCSource<int> source = new(new("source"), sut);
        monitor.Add(source);
        await source.OnNext(10, cts.Token);
        await source.OnNext(30, cts.Token);
        await source.OnComplete(cts.Token);
        var actual = await sink.GetResultAsync(cts.Token);

        await Assert.That(actual).IsEquivalentTo([
            "10-0", "10-1", "10-2", "10-3", "10-4", "10-5", "10-6", "10-7", "10-8", "10-9",
            "30-0", "30-1", "30-2", "30-3", "30-4", "30-5", "30-6", "30-7", "30-8", "30-9",
            "30-10", "30-11", "30-12", "30-13", "30-14", "30-15", "30-16", "30-17", "30-18", "30-19",
            "30-20", "30-21", "30-22", "30-23", "30-24", "30-25", "30-26", "30-27", "30-28", "30-29",
        ]);
        cts.Cancel();
    }

    // --- 002 ---

    public class SutProcessorTracking002
        : BCProcessorTracking<int, string, BCTracking<int, string>> {
        private readonly TestWorker002 _Worker;
        private readonly BCTrackingConsumer<int, string> _TrackingConsumer;

        public SutProcessorTracking002(
                BCDescription description,
                TestWorker002 worker,
                IBCConsumer<BCMessage<int, string>> nextConsumer
            ) : base(
                description, nextConsumer
            ) {
            this._Worker = worker;
            this._TrackingConsumer = new BCTrackingConsumer<int, string>(
                description,
                this.TrackingManager,
                nextConsumer);
        }

        protected override BCTracking<int, string> CreateRequest(
            int Value
            ) {
            return new BCTracking<int, string>(
                description: this.Description,
                Value: Value,
                nextTrackingConsumer: this.NextTrackingConsumer);
        }

        protected override async Task SendRequest(
            BCTracking<int, string> tracking,
            CancellationToken cancellationToken) {
            await this._Worker.Enqueue(tracking, cancellationToken);
        }
    }

    public class TestWorker002 {
        private readonly Channel<BCTracking<int, string>> _Channel;
        private Task? _TaskRunning;

        public TestWorker002() {
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
                    await Task.Run(() => Handle(tracking, cancellationToken), cancellationToken);
                }
            }

            static async void Handle(BCTracking<int, string> tracking, CancellationToken cancellationToken) {
                System.Console.Out.WriteLine($"Workter002:{tracking.Value}:Start");
                foreach (var i in System.Linq.Enumerable.Range(0, tracking.Value)) {
                    await tracking.OnNext(i.ToString(), cancellationToken);
                    await Task.Delay(((i % 10) + 1) * 10).ConfigureAwait(false);
                }
                await tracking.OnComplete(cancellationToken);
                System.Console.Out.WriteLine($"Workter002:{tracking.Value}:End");
            }
        }
    }

}
