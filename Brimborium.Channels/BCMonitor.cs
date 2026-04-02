#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO - change this to ILogger
/// </summary>
public class BCMonitor {
    public BCMonitor() {
    }

    public void AddMonitored(IBCMonitored monitored) {
        if (monitored.SetMonitor(this)) { 
            this.Log(monitored, "Monitor", "Add");
        }
    }

    public BCMonitorLogScope LogEnter(IBCPart part, string name) {
        this.Log(part, name, "Start");
        return new BCMonitorLogScope(this, part, name);
    }

    //public BCMonitorLogScope LogOnNextEnter<T>(IBCMonitored monitored, T value) {
    //    this.Log(monitored, "OnNext", "Start");
    //}

    public void Log(IBCPart part, string name, string kind) {
        System.Console.WriteLine($"{part.GetType().Name}.{name}:{kind}");
    }

    internal void LogAwait(IBCPart part, string name) {
        System.Console.WriteLine($"{part.GetType().Name}.{name}:await");
    }
}

public sealed record LogItem(IBCMonitored Part, string Name, string Kind);

public record struct BCMonitorLogScope(BCMonitor Monitor, IBCPart Part, string Name)
    : IDisposable {
    public void Dispose() {
        this.Monitor.Log(this.Part, this.Name, "End");
    }
}
public static class BCMonitorExtension {
    extension(BCMonitor thatMonitor) {
        public BCMonitor Add(IBCPart part) {
            if (part is IBCMonitored monitored) {
                thatMonitor.AddMonitored(monitored);
            } else {
                System.Console.WriteLine($"{part.GetType().FullName} is not IBCMonitored");
            }
            return thatMonitor;
        }
    }
}