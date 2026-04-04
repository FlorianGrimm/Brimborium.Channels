namespace Brimborium.Channels;

public class BCDescriptionNode {

    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Parent { get; set; } = string.Empty;
    public List<string>? Incoming { get; set; }
    public List<string>? Outgoing { get; set; }
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

    public void AddIncoming(IBCPart part) {
        if ((part is IBCMonitored monitored)
            && (monitored.GetNodeId() is string nodeId)) {
            (this.Incoming ??= new()).Add(nodeId);
        }
    }

    public void AddOutgoing(IBCPart part) {
        if ((part is IBCMonitored monitored)
            && (monitored.GetNodeId() is string nodeId)) {
            (this.Outgoing ??= new()).Add(nodeId);
        }
    }
}

public sealed class BCDescriptionGraph {
    public Dictionary<string, BCDescriptionNode> Nodes { get; } = new();

    public void SetParent(IBCPart child, BCDescriptionNode parent) {
        if (child is IBCMonitored childMonitored
            && childMonitored.GetNodeId() is { }  childNodeId) {
            if (this.Nodes.TryGetValue(childNodeId, out var childNode)) {
                childNode.Parent = parent.NodeId;
            }
        }
    }
}