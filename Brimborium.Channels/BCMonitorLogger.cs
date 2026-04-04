#pragma warning disable IDE1006 // Naming Styles

using Microsoft.Extensions.Logging;

namespace Brimborium.Channels;

public class BCMonitorLogger : IBCMonitor {
    public BCMonitorLogger(
            ILogger logger
        ) {
        
    }

    public void AddMonitored(IBCMonitored monitored) {
        throw new NotImplementedException();
    }

    public void Log(IBCPart part, string name, string kind) {
        throw new NotImplementedException();
    }

    public BCMonitorLogScope LogEnter(IBCPart part, string name) {
        throw new NotImplementedException();
    }
}