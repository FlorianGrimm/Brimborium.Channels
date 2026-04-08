namespace Brimborium.Channels;

[Tapper.TranspilationSource]
public sealed class BCDescriptionNode {

    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Parent { get; set; } = string.Empty;
    public List<BCPortNode>? Incoming { get; set; }
    public List<BCPortNode>? Outgoing { get; set; }
    public string NodeId { get => this._NodeId ?? string.Empty; set => this._NodeId = value; }

    private string? _NodeId;

    public string GetNodeId() {
        return this._NodeId ??= Guid.NewGuid().ToString();
    }

    public void SetParent(IBCMonitored monitored) {
        if (this.GetNodeId() is string nodeId) {
            this.Parent = nodeId;
        }
    }

    public void AddIncoming(string port, IBCPart part) {
        if ((part is IBCMonitored monitored)
            && (monitored.GetNodeId() is string nodeId)) {
            (this.Incoming ??= new()).Add(new() { PortId = port, NodeId = nodeId });
        }
    }

    public void AddOutgoing(string port, IBCPart part) {
        if ((part is IBCMonitored monitored)
            && (monitored.GetNodeId() is string nodeId)) {
            (this.Outgoing ??= new()).Add(new() { PortId=port, NodeId=nodeId });
        }
    }
}
[Tapper.TranspilationSource]
public sealed class BCPortNode {
    public required string PortId { get; set; }
    public required string NodeId { get; set; }
}