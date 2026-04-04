#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Base interface for all pipeline parts (sources, processors, consumers).
/// Provides identity, lifetime tracking, and completion-wait primitives.
/// </summary>
public interface IBCPart {
    /// <summary>
    /// Human-readable name that identifies this part in logs and diagnostics.
    /// </summary>
    BCDescription Description { get; }

    /// <summary>
    /// The current lifetime state of this part (<see cref="BCLifeTime.Active"/>,
    /// <see cref="BCLifeTime.Completing"/>, or <see cref="BCLifeTime.Completed"/>).
    /// </summary>
    BCLifeTime LifeTime { get; }

    /// <summary>
    /// Returns a task that completes when this part's own work has finished
    /// (i.e. its <see cref="LifeTime"/> has reached <see cref="BCLifeTime.Completed"/>).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    Task WaitSelfCompletedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns a task that completes when this part and all downstream (right-side)
    /// parts it is connected to have reached <see cref="BCLifeTime.Completed"/>.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    Task WaitRightCompletedAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Abstract base class for pipeline parts, implementing the common <see cref="IBCPart"/> contract.
/// Subclasses implement <see cref="WaitSelfCompletedAsync"/> and <see cref="WaitRightCompletedAsync"/>.
/// </summary>
public abstract class BCPart : IBCPart {
    protected BCLifeTime _LifeTime;

    /// <param name="description">Human-readable name used in logs and diagnostics.</param>
    public BCPart(
        BCDescription description) {
        this.Description = description;
    }

    /// <inheritdoc/>
    public BCDescription Description { get; }

    /// <inheritdoc/>
    public BCLifeTime LifeTime => this._LifeTime;

    /// <summary>Transitions <see cref="LifeTime"/> from <see cref="BCLifeTime.Active"/> to <see cref="BCLifeTime.Completing"/>.</summary>
    /// <returns><c>true</c> if the transition occurred; <c>false</c> if it was already completing or completed.</returns>
    protected bool SetCompleting() {
        return BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
    }

    /// <summary>Transitions <see cref="LifeTime"/> from <see cref="BCLifeTime.Completing"/> to <see cref="BCLifeTime.Completed"/>.</summary>
    /// <returns><c>true</c> if the transition occurred; <c>false</c> if it was not in the completing state.</returns>
    protected bool SetCompleted() {
        return BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
    }

    /// <inheritdoc/>
    public abstract Task WaitSelfCompletedAsync(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task WaitRightCompletedAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Abstract base class for pipeline parts that support monitoring via <see cref="IBCMonitor"/>.
/// Extends <see cref="BCPart"/> and implements <see cref="IBCMonitored"/>.
/// </summary>
public abstract class BCPartMonitored
    : BCPart
    , IBCMonitored {
    /// <summary>The attached monitor, or <c>null</c> if none has been set.</summary>
    protected IBCMonitor? _Monitor;

    /// <param name="description">Human-readable name used in logs and diagnostics.</param>
    protected BCPartMonitored(
        BCDescription description
    ) : base(
        description
    ) {
    }

    /// <inheritdoc/>
    IBCMonitor? IBCMonitored.GetMonitor() => this._Monitor;

    /// <inheritdoc/>
    public virtual bool SetMonitor(IBCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        return true;
    }

    public virtual void Describe(BCDescriptionGraph description) { }
}