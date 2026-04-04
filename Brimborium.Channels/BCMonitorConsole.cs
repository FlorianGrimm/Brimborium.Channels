#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Diagnostic observer that can be attached to pipeline parts.
/// Logs named Start/End scopes for every operation to the console.
/// Cascades to all downstream parts when attached via <see cref="IBCMonitored.SetMonitor"/>.
/// </summary>
/// <remarks>Intended to be replaced by an <c>ILogger</c>-based implementation in the future.</remarks>
public class BCMonitorConsole : IBCMonitor {
    public BCMonitorConsole() {
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

    public void Log(IBCPart part, string name, string kind) {
        System.Console.WriteLine($"{part.Description.Name}.{name}:{kind}");
    }

    internal void LogAwait(IBCPart part, string name) {
        System.Console.WriteLine($"{part.Description.Name}.{name}:await");
    }
}
