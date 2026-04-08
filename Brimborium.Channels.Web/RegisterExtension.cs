using Microsoft.AspNetCore.Builder;

namespace Brimborium.Channels {
    public static class WebRegisterExtension {
        public static void RegisterHubs(
            global::Microsoft.AspNetCore.Builder.WebApplicationBuilder builder
            ) {
        }

        public static void MapHubs(WebApplication app) {
            app.MapHub<Hubs.BCVisualizationHub>("/_hubs/BCVisualizationHub");
        }
    }
}
