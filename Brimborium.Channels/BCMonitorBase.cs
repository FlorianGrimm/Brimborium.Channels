#pragma warning disable IDE1006 // Naming Styles
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace Brimborium.Channels;

public class BCMonitorBase : IBCMonitor {
    private ConcurrentDictionary<IBCMonitored, IBCMonitored> _Dict = new();

    public BCMonitorBase() {
    }
        
    public virtual void AddMonitored(IBCMonitored monitored) {
        this._Dict.TryAdd(monitored, monitored);
    }

    public void Log(IBCPart part, string name, string kind) {
    }

    public BCMonitorLogScope LogEnter(IBCPart part, string name) {
        this.Log(part, name, "Start");
        return new BCMonitorLogScope(this, part, name);
    }

    public BCDescriptionGraph Describe() {
        BCDescriptionGraph result = new ();
        foreach (var monitored in this._Dict.Keys) {
            monitored.Describe(result);
        }
        return result;
    }
}
public class BCDescriptionGraph { 
}