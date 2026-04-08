using Microsoft.Extensions.Logging;

namespace Brimborium.Channels.Hubs;

//[Injectio.Attributes.RegisterSingleton<IBCMonitoringServices>]
public class BCMonitoringServices
    : IBCMonitoringServices {
    private readonly ILogger _Logger;

    public BCMonitoringServices(
        ILogger<BCMonitoringServices> logger
        ) {
        this._Logger = logger;
    }

    public virtual IBCMonitor CreateMonitor() {
        return new BCMonitorLogger(this._Logger);
    }
}