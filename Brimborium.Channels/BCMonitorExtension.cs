#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>Extension methods for <see cref="IBCMonitor"/> for convenient part registration.</summary>
public static class BCMonitorExtension {
    extension(IBCMonitor thatMonitor) {
        public IBCMonitor Add(IBCPart part) {
            if (part is IBCMonitored monitored) {
                thatMonitor.AddMonitored(monitored);
            } else {
                System.Console.WriteLine($"{part.GetType().FullName} is not IBCMonitored");
            }
            return thatMonitor;
        }
    }
}