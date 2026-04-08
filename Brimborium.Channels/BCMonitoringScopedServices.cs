using Microsoft.Extensions.Logging;

namespace Brimborium.Channels.Hubs;

//[Injectio.Attributes.RegisterSingleton<IBCMonitoringScopedServices>]
public class BCMonitoringScopedServices
    : IBCMonitoringScopedServices {
    private readonly ILogger _Logger;

    public BCMonitoringScopedServices(
        ILogger<BCMonitoringScopedServices> logger
        ) {
        this._Logger = logger;
    }

    public virtual IBCMonitor CreateMonitor() {
        return new BCMonitorLogger(this._Logger);
    }
}