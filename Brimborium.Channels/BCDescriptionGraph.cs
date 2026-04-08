namespace Brimborium.Channels;

[Tapper.TranspilationSource]
public sealed class BCDescriptionGraph {
    public required string GraphId { get; set; }

    public Dictionary<string, BCDescriptionNode> Nodes { get; } = new();

    public void SetParent(IBCPart child, BCDescriptionNode parent) {
        if (child is IBCMonitored childMonitored
            && childMonitored.GetNodeId() is { } childNodeId) {
            lock (this) {
                if (this.Nodes.TryGetValue(childNodeId, out var childNode)) {
                    childNode.Parent = parent.NodeId;
                }
            }
        }
    }
}