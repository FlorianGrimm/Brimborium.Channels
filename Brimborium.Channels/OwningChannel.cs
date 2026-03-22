namespace Brimborium.Channels;

public sealed partial class OwningChannel<T> {
    private readonly Channel<T> _Channel;
    private readonly string _Name;
    private OwnedChannelWriter<T>? _ChannelWriter;
    private OwnedChannelReader<T>? _ChannelReader;

    public OwningChannel(Channel<T> channel, string name) {
        this._Channel = channel;
        this._Name = name;
    }

    public OwnedChannelWriter<T> GetWriter() {
        if (this._ChannelWriter is { }) {
            throw new InvalidOperationException("GetWriter must be called one time.");
        }
        OwnedChannelWriter<T> result = new(this._Channel.Writer, this._Name);
        this._ChannelWriter = result;
        return result;
    }

    public OwnedChannelReader<T> GetReader() {
        if (this._ChannelReader is { }) {
            throw new InvalidOperationException("GetReader must be called one time.");
        }
        OwnedChannelReader<T> result = new(this._Channel.Reader, this._Name);
        this._ChannelReader = result;
        return result;
    }
}

#region InvokeProducer

public sealed partial class OwningChannel<T> {

    public ProducerVoid InvokeProducerVoid(
        Func<
            SharedChannelWriter<T> /*channelWriter*/,
            CancellationToken /*cancellationToken*/,
            /*async*/ Task
            > asyncProducer,
            CancellationToken cancellationToken
        ) {
        var taskProducer = InvokeProducerAndComplete(this.GetWriter(), asyncProducer, cancellationToken);
        return new ProducerVoid(
            this.GetReader(),
            taskProducer);

        static async Task InvokeProducerAndComplete(
            OwnedChannelWriter<T> channelWriter,
            Func<
                SharedChannelWriter<T> /*channelWriter*/,
                CancellationToken /*cancellationToken*/,
                /*async*/ Task
                > asyncProducer,
            CancellationToken cancellationToken
            ) {
            try {
                await asyncProducer(channelWriter.AsShared(), cancellationToken);
                channelWriter.CompleteSuccess();
            } catch (System.Exception error) {
                channelWriter.CompleteFailed(error);
                throw;
            }
        }
    }

    public readonly struct ProducerVoid {
        private readonly OwnedChannelReader<T> _ChannelReader;
        private readonly Task _TaskProducer;

        internal ProducerVoid(
            OwnedChannelReader<T> channelReader,
            Task taskProducer) {
            this._ChannelReader = channelReader;
            this._TaskProducer = taskProducer;
        }

        public Consumer<ProducerResult> InvokeConsumer<ProducerResult>(
            Func<
                OwnedChannelReader<T> /* channelReader */,
                CancellationToken /* cancellationToken */,
                /*async*/ Task<ProducerResult>
                > asyncConsumer,
            CancellationToken cancellationToken
            ) {
            var taskConsumer = asyncConsumer(
                this._ChannelReader,
                cancellationToken);

            return new Consumer<ProducerResult>(
                this._TaskProducer,
                taskConsumer
                );
        }
    }
    public readonly struct Consumer<ProducerResult> {
        private readonly Task _TaskProducer;
        private readonly Task<ProducerResult> _TaskConsumer;

        internal Consumer(
            Task taskProducer,
            Task<ProducerResult> taskConsumer) {
            this._TaskProducer = taskProducer;
            this._TaskConsumer = taskConsumer;
        }

        public async readonly Task<ProducerResult> RunAsync(bool continueOnCapturedContext) {
            await this._TaskProducer.ConfigureAwait(continueOnCapturedContext);
            var result = await this._TaskConsumer.ConfigureAwait(continueOnCapturedContext);
            return result;
        }
    }
}

#endregion InvokeProducer

#region InvokeProducerWithResult

public sealed partial class OwningChannel<T> {

    public ProducerWithResult<ProducerResult> InvokeProducerWithResult<ProducerResult>(
        Func<
            SharedChannelWriter<T> /*channelWriter*/,
            CancellationToken /*cancellationToken*/,
            /*async*/ Task<ProducerResult>
            > asyncProducer,
            CancellationToken cancellationToken
        ) {
        var taskProducer = InvokeProducerAndComplete(this.GetWriter(), asyncProducer, cancellationToken);
        return new ProducerWithResult<ProducerResult>(
            this.GetReader(),
            taskProducer);

        static async Task<ProducerResult> InvokeProducerAndComplete(
            OwnedChannelWriter<T> channelWriter,
            Func<
                SharedChannelWriter<T> /*channelWriter*/,
                CancellationToken /*cancellationToken*/,
                /*async*/ Task<ProducerResult>
                > asyncProducer,
            CancellationToken cancellationToken
            ) {
            try {
                var result = await asyncProducer(channelWriter.AsShared(), cancellationToken);
                channelWriter.CompleteSuccess();
                return result;
            } catch (System.Exception error) {
                channelWriter.CompleteFailed(error);
                throw;
            }
        }
    }

    public readonly struct ProducerWithResult<ProducerResult> {
        private readonly OwnedChannelReader<T> _ChannelReader;
        private readonly Task<ProducerResult> _TaskProducer;

        internal ProducerWithResult(
            OwnedChannelReader<T> channelReader,
            Task<ProducerResult> taskProducer) {
            this._ChannelReader = channelReader;
            this._TaskProducer = taskProducer;
        }

        public ConsumerWithResult<ProducerResult, ConsumerResult> InvokeConsumer<ConsumerResult>(
            Func<
                OwnedChannelReader<T> /* channelReader */,
                CancellationToken /* cancellationToken */,
                /*async*/ Task<ConsumerResult>
                > asyncConsumer,
            CancellationToken cancellationToken
            ) {
            var taskConsumer = asyncConsumer(
                this._ChannelReader,
                cancellationToken);

            return new ConsumerWithResult<ProducerResult, ConsumerResult>(
                taskProducer: this._TaskProducer,
                taskConsumer: taskConsumer
                );
        }
    }
    public readonly struct ConsumerWithResult<ProducerResult, ConsumerResult> {
        private readonly Task<ProducerResult> _TaskProducer;
        private readonly Task<ConsumerResult> _TaskConsumer;

        internal ConsumerWithResult(
            Task<ProducerResult> taskProducer,
            Task<ConsumerResult> taskConsumer
            ) {
            this._TaskProducer = taskProducer;
            this._TaskConsumer = taskConsumer;
        }

        public async readonly Task<ConsumerProducerResult<ProducerResult, ConsumerResult>> RunAsync(bool continueOnCapturedContext) {
            var producerResult = await this._TaskProducer.ConfigureAwait(continueOnCapturedContext);
            var consumerResult = await this._TaskConsumer.ConfigureAwait(continueOnCapturedContext);
            return new(producerResult, consumerResult);
        }
    }
}

public record struct ConsumerProducerResult<ProducerResult, ConsumerResult>(
    ProducerResult producerResult, ConsumerResult consumerResult);

#endregion InvokeProducerWithResult