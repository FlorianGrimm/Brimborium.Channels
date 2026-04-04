#pragma warning disable IDE1006 // Naming Styles
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace Brimborium.Channels;

public class BCMonitorBase : IBCMonitor {
    private readonly ConcurrentDictionary<IBCMonitored, BCDescriptionNode> _Dict = new();
    private readonly List<BCLogItem> _ListLogItem = new();

    public BCMonitorBase() {
    }

    public virtual void AddMonitored(IBCMonitored monitored) {
        var nodeId = monitored.GetNodeId();
        BCDescriptionNode node = new BCDescriptionNode();
        if (nodeId is { }) {
            node.NodeId = nodeId;
            this._Dict.TryAdd(monitored, node);
        } else {
            nodeId = node.GetNodeId();
            monitored.SetNodeId(nodeId);
            this._Dict.TryAdd(monitored, node);
        }
    }

    public void Log(IBCPart part, string name, string kind) {
        if (part is IBCMonitored monitored
            && this._Dict.TryGetValue(monitored, out var node)) {
            lock (this._ListLogItem) {
                this._ListLogItem.Add(new BCLogItem(part, name, kind));
            }
        }
    }

    public BCMonitorLogScope LogEnter(IBCPart part, string name) {
        this.Log(part, name, "Start");
        return new BCMonitorLogScope(this, part, name);
    }

    public BCDescriptionGraph Describe() {
        BCDescriptionGraph result = new();
        foreach (var (part, node) in this._Dict) {
            result.Nodes.Add(node.GetNodeId(), node);
        }
        foreach (var (part, node) in this._Dict) {
            if (part is IBCMonitored monitored) {
                monitored.Describe(node, result);
            }
        }
        return result;
    }
}