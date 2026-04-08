#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;

namespace Brimborium.Channels;

public class BCMonitorBase : IBCMonitor {
    private readonly ConcurrentDictionary<IBCMonitored, BCDescriptionNode> _Dict = new();
    private string? _GraphId;

    public string GraphId {
        get => this._GraphId ??= Guid.NewGuid().ToString();
        set => this._GraphId = value;
    }

    public BCMonitorBase() {
    }

    public virtual void AddMonitored(IBCMonitored monitored) {
        var nodeId = monitored.GetNodeId();
        BCDescriptionNode node = new BCDescriptionNode();
        bool wasAdded;
        if (nodeId is { }) {
            node.NodeId = nodeId;
            wasAdded = this._Dict.TryAdd(monitored, node);
        } else {
            nodeId = node.GetNodeId();
            monitored.SetNodeId(nodeId);
            wasAdded = this._Dict.TryAdd(monitored, node);
        }
        if (wasAdded) {
            monitored.SetMonitor(this);
        }
    }

    public void Log(IBCPart part, string name, string kind) {
        if (part is IBCMonitored monitored
            && this._Dict.TryGetValue(monitored, out var node)) {
            this.Write(new BCLogItem(part, name, kind));
        }
    }

    public virtual void Write(in BCLogItem logItem) { }

    public BCMonitorLogScope LogEnter(IBCPart part, string name) {
        this.Log(part, name, "Start");
        return new BCMonitorLogScope(this, part, name);
    }

    public BCDescriptionGraph Describe(
        BCDescriptionGraph? descriptionGraph = default
        ) {
        if (descriptionGraph is null) {
            descriptionGraph = new() {
                GraphId = this.GraphId
            };
        }
        List<KeyValuePair<IBCMonitored, BCDescriptionNode>> listTodo;
        lock (descriptionGraph) {
            listTodo = new(this._Dict.Count);
            foreach (var (part, node) in this._Dict) {
                if (descriptionGraph.Nodes.TryAdd(node.GetNodeId(), node)) {
                    listTodo.Add(new(part, node));
                }
            }
        }
        foreach (var (part, node) in listTodo) {
            if (part is IBCMonitored monitored) {
                monitored.Describe(node, descriptionGraph);
            }
        }
        return descriptionGraph;
    }
}