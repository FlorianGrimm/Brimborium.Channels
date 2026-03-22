# Brimborium.Channels

Experiment

Sample

```CSharp
    var (resultProducer, resultConsumer) = await  Channel.CreateUnbounded<int>()
        .AsOwningChannel("Name")
        .InvokeProducerWithResult(
            asyncProducer:(channelWriter, cancellationToken) => Simple3Producer(channelWriter, cancellationToken),
            cancellationToken: cancellationToken
        )
        .InvokeConsumer(
            asyncConsumer:(channelReader, cancellationToken)=> Simple3Consumer(channelReader, cancellationToken),
            cancellationToken: cancellationToken
        )
        .RunAsync(true)
        ;
```