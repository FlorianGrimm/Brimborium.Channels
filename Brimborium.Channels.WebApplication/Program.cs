// using AspNetCore.SignalR.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TypedSignalR.Client.DevTools;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.SignalR;

namespace Brimborium.Channels.WebApplication {
    public class Program {
        public static void Main(string[] args) {
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            var locationUI = Brimborium.Channels.FrontEnd.FrontendLocation.GetLocationUI();
            bool existsLocationUI = System.IO.Directory.Exists(locationUI);
            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR()
                .AddJsonProtocol((jsonHubProtocolOptions) => { 
                })
                ;
            //.AddHubInstrumentation();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            WebRegisterExtension.RegisterHubs(builder);
            builder.Services
                .AddBrimboriumChannelsWeb()
                .AddBrimboriumChannelsWebApplication();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseSignalRHubSpecification();
            app.UseSignalRHubDevelopmentUI();

            app.UseRouting();

            app.UseAuthorization();
            var fileProviderUI = new PhysicalFileProvider(locationUI);
            app.Use(async (context, next) =>
            {
                var url = context.Request.Path;
                if (url is { } requestPath
                    && requestPath.StartsWithSegments(new PathString("/ui"), out var remaining)
                    && !remaining.HasValue) {
                    
                    await context.Response.SendFileAsync(fileProviderUI.GetFileInfo("index.html"));
                    
                    return;
                }
                await next();
            });
            if (existsLocationUI) {
                app.UseStaticFiles();
                app.UseStaticFiles(
                    new StaticFileOptions() {
                        RequestPath = "/ui",
                        FileProvider = fileProviderUI
                    });
            } else { 
                app.MapStaticAssets();
            }
            WebRegisterExtension.MapHubs(app);

            app.Services.GetRequiredService<SampleMinimalApi>().Map(app);

            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
