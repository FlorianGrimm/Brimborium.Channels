#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// Extended <see cref="IBCPart"/> that supports attaching a <see cref="IBCMonitor"/> for logging and diagnostics.
/// </summary>
public interface IBCMonitored : IBCPart {

    /// <summary>Returns the currently attached monitor, or <c>null</c> if none has been set.</summary>
    IBCMonitor? GetMonitor();

    /// <summary>
    /// Attaches a monitor to this part and cascades it to downstream (right-side) consumers.
    /// </summary>
    /// <param name="monitor">The monitor to attach.</param>
    /// <returns><c>true</c> if the monitor was set; <c>false</c> if one was already attached.</returns>
    bool SetMonitor(IBCMonitor monitor);

    /// <summary>
    /// Set information to node.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="descriptionGraph"></param>
    void Describe(BCDescriptionNode node, BCDescriptionGraph descriptionGraph);

    /// <summary>
    /// NodeId of BCDescriptionNode
    /// </summary>
    string? GetNodeId();

    /// <summary>
    /// NodeId of BCDescriptionNode
    /// </summary>
    void SetNodeId(string nodeId);
}