namespace Brimborium.Channels.Hubs;

[Injectio.Attributes.RegisterSingleton<IBCMonitoringServices>]
public class BCMonitoringWebServices
    : IBCMonitoringServices {
    public IBCMonitor CreateMonitor() {
        return new BCMonitorToLogRecord();
    }
}

[Injectio.Attributes.RegisterSingleton<IBCMonitoringScopedServices>]
public class BCMonitoringScopedWebServices
    : IBCMonitoringScopedServices {
    public IBCMonitor CreateMonitor() {
        return new BCMonitorToLogRecord();
    }
}