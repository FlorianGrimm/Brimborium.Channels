using System.Threading.Channels;

namespace Brimborium.Channels.Hubs;

[TypedSignalR.Client.Hub]
public interface IBCVisualizationHub {
    Task<BCDescriptionGraph?> GetDescriptionGraph(
            string graphId
        );

    Task<ChannelReader<BCDescriptionGraph>> DescriptionGraphChannel(
            CancellationToken cancellation
        );

    Task<ChannelReader<BCLogRecord>> LogRecordChannel(
            CancellationToken cancellation
        );

}

[TypedSignalR.Client.Receiver]
public interface IBCVisualizationReveiver {
    Task OnJoin();

    Task OnLeave();

    Task OnMessage(List<BCLogRecord> listLogRecords);
}
