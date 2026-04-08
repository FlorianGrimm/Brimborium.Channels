using Brimborium.Channels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[Injectio.Attributes.RegisterSingleton]
public class SampleMinimalApi {
    private readonly IBCMonitoringServices _MonitoringServices;
    private readonly ILogger<SampleMinimalApi> _Logger;

    public SampleMinimalApi(
        IBCMonitoringServices monitoringServices,
        ILogger<SampleMinimalApi> logger) {
        this._MonitoringServices = monitoringServices;
        this._Logger = logger;

    }

    public void Map(WebApplication app) {
        app.MapGet("/api/sample", ApiSample);
    }

    public async Task<string> ApiSample(
        int value,
        HttpContext httpContext,
        [FromServices] IBCMonitoringScopedServices monitoringScopedServices
        ) {
        var result = await this.RunAsync(value, httpContext.RequestAborted);
        return result;
    }

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

    public async Task<string> RunAsync(int value, CancellationToken cancellationToken) {
        var monitor = new BCMonitorToLogRecord();

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
        monitor.Add(source);

        //await source.OnNextEnumerable([1, 2, 3, 4, 5], cancellationToken);
        for (int i = 1; i <= value; i++) {
            await source.OnNext(i, cancellationToken);
            await Task.Delay(i);
        }
        await source.OnComplete(cancellationToken);

        _ = await sinkSum.GetResultAsync();
        _ = await sinkAvg.GetResultAsync();
        var descriptionGraph = monitor.Describe(default);
        var listLogRecord = monitor.GetAndClearListLogItem();

        return System.Text.Json.JsonSerializer.Serialize(
            new {
                descriptionGraph,
                listLogRecord
            },
            new JsonSerializerOptions() { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }
}