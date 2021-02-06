using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// OpenTelemetry Refs
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Messaging Utils
using Utils.Messaging;

using TodoMvcUi.Controllers;

namespace TodoMvcUi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Add instance of Messaging Utils' MessageSender
            services.AddSingleton<MessageSender>();

            // Updated to use OTLP, otel-collector
            // Adding the OtlpExporter creates a GrpcChannel.
            // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
            // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // TODO: Setup env vars for service.name, service.namespace, e.g.,
            // var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "TodoMvcUi";
            // var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "kjt-Otel-ToDo";
            // ...
            //    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName,serviceNamespace))
            // ...

            // Add OpenTelemetry Console Exporter & Jaeger Exporter - 0.7.0-beta
            //services.AddOpenTelemetryTracerProvider((builder) => builder
            // 1.0.0-rc1.1
            services.AddOpenTelemetryTracing((builder) => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TodoMvcUi","kjt-Otel-ToDo"))
                .AddSource(nameof(MessageSender), nameof(HomeController))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
                //.AddJaegerExporter(jaeger =>
                //{
                    //jaeger.ServiceName = this.Configuration.GetValue<string>("Jaeger:ServiceName");
                    //jaeger.AgentHost = this.Configuration.GetValue<string>("Jaeger:Host");
                    //jaeger.AgentPort = this.Configuration.GetValue<int>("Jaeger:Port");
                    //jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoMvcUi";
                    // When I move to env vars, it'll look something like this:
                    //jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "localhost";
                    //jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "host.docker.internal";
                    //jaeger.AgentPort = 6831;
                //})
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://host.docker.internal:4317");
                })
                .SetSampler(new AlwaysOnSampler())
                );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
