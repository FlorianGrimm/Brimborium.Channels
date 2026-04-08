#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

public interface IBCMonitor {
    void AddMonitored(IBCMonitored monitored);
    void Log(IBCPart part, string name, string kind);
    BCMonitorLogScope LogEnter(IBCPart part, string name);
}

/// <summary>Immutable log record produced by <see cref="IBCMonitor"/> for a single named event.</summary>
public record struct BCLogItem(
    DateTime Timestamp,
    IBCPart Part, string Name, string Kind
    ) {
    public BCLogItem(
            IBCPart Part, string Name, string Kind
        ) : this(
            DateTime.UtcNow,
            Part, Name, Kind
        ) {
    }
}

[Tapper.TranspilationSource]
public record struct BCLogRecord(
    DateTime Timestamp,
    string NodeId, string Name, string Kind
    );

/// <summary>
/// Disposable scope returned by <see cref="IBCMonitor.LogEnter"/>.
/// Logs a <c>Start</c> event on creation and an <c>End</c> event on disposal.
/// </summary>
public record struct BCMonitorLogScope(IBCMonitor Monitor, IBCPart Part, string Name)
    : IDisposable {
    public void Dispose() {
        this.Monitor.Log(this.Part, this.Name, "End");
    }
}
