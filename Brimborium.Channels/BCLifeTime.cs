#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Represents the three lifecycle stages of a pipeline part.
/// Transitions are strictly one-way: <see cref="Active"/> → <see cref="Completing"/> → <see cref="Completed"/>.
/// </summary>
public enum BCLifeTime {
    /// <summary>
    /// After creation Subscripe OnNext OnError does not change this.
    /// </summary>
    Active,

    /// <summary>
    /// OnComplete was called, may be other Incoming Connections are still Active, actions may be still pending.
    /// </summary>
    Completing,

    /// <summary>
    /// If Completing and all work is done. OnComplete to the next is being send or was send.
    /// </summary>
    Completed
}

/// <summary>
/// Thread-safe helpers that atomically advance a <see cref="BCLifeTime"/> field
/// through its one-way transitions using <see cref="System.Threading.Interlocked.CompareExchange"/>.
/// </summary>
public static class BCLifeTimeExtension {
    /// <summary>
    /// TODO
    /// </summary>
    public static bool SetCompleting(ref BCLifeTime lifeTimeField) {
        return (BCLifeTime.Active == lifeTimeField)
            && (BCLifeTime.Active == System.Threading.Interlocked.CompareExchange(ref lifeTimeField, BCLifeTime.Completing, BCLifeTime.Active));
    }

    /// <summary>
    /// TODO
    /// </summary>
    public static bool SetCompleted(ref BCLifeTime lifeTimeField) {
        return (BCLifeTime.Completing == lifeTimeField)
            && (BCLifeTime.Completing == System.Threading.Interlocked.CompareExchange(ref lifeTimeField, BCLifeTime.Completed, BCLifeTime.Completing));
    }
}