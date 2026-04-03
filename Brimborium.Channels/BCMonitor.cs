#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Diagnostic observer that can be attached to pipeline parts.
/// Logs named Start/End scopes for every operation to the console.
/// Cascades to all downstream parts when attached via <see cref="IBCMonitored.SetMonitor"/>.
/// </summary>
/// <remarks>Intended to be replaced by an <c>ILogger</c>-based implementation in the future.</remarks>
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
        System.Console.WriteLine($"{part.Description.Name}.{name}:{kind}");
    }

    internal void LogAwait(IBCPart part, string name) {
        System.Console.WriteLine($"{part.Description.Name}.{name}:await");
    }
}

/// <summary>Immutable log record produced by <see cref="BCMonitor"/> for a single named event.</summary>
public sealed record LogItem(IBCMonitored Part, string Name, string Kind);

/// <summary>
/// Disposable scope returned by <see cref="BCMonitor.LogEnter"/>.
/// Logs a <c>Start</c> event on creation and an <c>End</c> event on disposal.
/// </summary>
public record struct BCMonitorLogScope(BCMonitor Monitor, IBCPart Part, string Name)
    : IDisposable {
    public void Dispose() {
        this.Monitor.Log(this.Part, this.Name, "End");
    }
}
/// <summary>Extension methods for <see cref="BCMonitor"/> for convenient part registration.</summary>
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