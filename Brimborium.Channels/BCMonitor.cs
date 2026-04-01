#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public class BCMonitor {
    public BCMonitor() {
    }

    public void AddMonitored(IBCMonitored monitored) {
        monitored.SetMonitor(this);
        this.Log(monitored, "Add", "");
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
}

public sealed record LogItem(IBCMonitored Part, string Name, string Kind);

public record struct BCMonitorLogScope(BCMonitor Monitor, IBCPart part, string Name)
    : IDisposable {
    public void Dispose() {
        Monitor.Log(part, Name, "End");
    }
}
public static class BCMonitorExtension {
    extension(BCMonitor thatMonitor) {
        public BCMonitor Add(IBCPart part) {
            if (part is IBCMonitored monitored) {
                thatMonitor.AddMonitored(monitored);
            }
            return thatMonitor;
        }
    }
}