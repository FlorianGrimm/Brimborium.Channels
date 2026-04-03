#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
public interface IBCPart {
    /// <summary>
    /// TODO
    /// </summary>
    BCDescription Description { get; }

    /// <summary>
    /// The current lifetime state
    /// </summary>
    BCLifeTime LifeTime { get; }

    /// <summary>
    /// The state switched to Complete.
    /// </summary>
    Task WaitCompletedAsync(CancellationToken cancellationToken);
}

public abstract class BCPart : IBCPart {
    protected BCLifeTime _LifeTime;

    public BCPart(
        BCDescription description) {
        this.Description = description;
    }
    public BCDescription Description { get; }

    public BCLifeTime LifeTime => this._LifeTime;

    protected bool SetCompleting() {
        return BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
    }

    protected bool SetCompleted() {
        return BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
    }

    public abstract Task WaitCompletedAsync(CancellationToken cancellationToken);
}

public abstract class BCPartMonitored
    : BCPart
    , IBCMonitored {
    protected BCMonitor? _Monitor;

    protected BCPartMonitored(
        BCDescription description
    ) : base(
        description
    ) {
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public virtual bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        return true;
    }
}