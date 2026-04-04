#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Marker interface for composite pipeline blocks that group
/// one or more incoming consumers and outgoing producers behind a single unit.
/// </summary>
public interface IBCBlock : IBCPart {
}

/// <summary>
/// Abstract base class for composite pipeline blocks.
/// Maintains lists of incoming consumers and outgoing producers,
/// and coordinates lifetime and monitoring across all of them.
/// </summary>
public class BCBlock
    : BCPartMonitored
    , IBCBlock {
    protected List<IBCConsumer> ListIncoming = new();
    protected List<IBCProducer> ListOutgoing = new();

    protected BCBlock(
        BCDescription description
    ) : base(
        description
    ) {
    }

    protected bool IsIncomingCompleted() {
        foreach (var incoming in this.ListIncoming) {
            if (incoming.LifeTime is BCLifeTime.Completed) {
                continue;
            } else {
                return false;
            }
        }
        return true;
    }

    public override async Task WaitSelfCompletedAsync(CancellationToken cancellationToken) {
        foreach (var outgoing in this.ListOutgoing) {
            await outgoing.WaitSelfCompletedAsync(cancellationToken);
        }
    }

    public override async Task WaitRightCompletedAsync(CancellationToken cancellationToken) {
        foreach (var outgoing in this.ListOutgoing) {
            await outgoing.WaitRightCompletedAsync(cancellationToken);
        }
    }

    internal new bool SetCompleting() {
        return BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
    }

    internal new bool SetCompleted() {
        return BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
    }

    public override bool SetMonitor(IBCMonitor monitor) {
        var result = base.SetMonitor(monitor);
        if (result) {
            foreach (var incoming in this.ListIncoming) {
                monitor.Add(incoming);
            }
            foreach (var outgoing in this.ListOutgoing) {
                monitor.Add(outgoing);
            }
        }
        return true;
    }

    protected void AddIncoming(IBCConsumer consumer) {
        this._Monitor?.Add(consumer);
    }
    protected void AddOutgoing(IBCProducer producer) {
        this._Monitor?.Add(producer);
    }

}
