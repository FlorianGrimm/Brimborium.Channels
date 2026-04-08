namespace Brimborium.Channels;

public interface IBCMonitoringCommonServices {
    IBCMonitor CreateMonitor();
}

public interface IBCMonitoringServices
    : IBCMonitoringCommonServices {
}

public interface IBCMonitoringScopedServices
    : IBCMonitoringCommonServices {
}