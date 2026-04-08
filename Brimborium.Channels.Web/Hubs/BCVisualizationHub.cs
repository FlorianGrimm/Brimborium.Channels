using System.Threading.Channels;

namespace Brimborium.Channels.Hubs;

public class BCVisualizationHub
    : Microsoft.AspNetCore.SignalR.Hub<IBCVisualizationReveiver>
    , IBCVisualizationHub
    // , IBCVisualizationReveiver 
    {

    public BCVisualizationHub() {
    }

    public override async Task OnConnectedAsync() {
        await base.OnConnectedAsync();
        var connectionId = this.Context.ConnectionId;
    }

    public override async Task OnDisconnectedAsync(Exception? exception) {
        var connectionId = this.Context.ConnectionId;
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<BCDescriptionGraph?> GetDescriptionGraph(string graphId) {
        await Task.CompletedTask;

        return new BCDescriptionGraph() {
            GraphId = graphId
        };
    }

    public Task<ChannelReader<BCDescriptionGraph>> DescriptionGraphChannel(
        CancellationToken cancellation) {
        throw new NotImplementedException();
    }

    public Task<ChannelReader<BCLogRecord>> LogRecordChannel(
        CancellationToken cancellation) {
        throw new NotImplementedException();
    }

    public Task OnJoin() {
        var connectionId = this.Context.ConnectionId;
        return Task.CompletedTask;
    }

    public Task OnLeave() {
        return Task.CompletedTask;
    }

    public async Task OnMessage(List<BCLogRecord> listLogRecords) {
        await this.Clients.All.OnMessage(listLogRecords);
    }
}
