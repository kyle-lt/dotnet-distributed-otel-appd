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

            // Add OpenTelemetry Console Exporter & Jaeger Exporter - 0.7.0-beta
            services.AddOpenTelemetryTracerProvider((builder) => builder
                .AddSource(nameof(MessageSender), nameof(HomeController))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
                .AddJaegerExporter(jaeger =>
                {
                    //jaeger.ServiceName = this.Configuration.GetValue<string>("Jaeger:ServiceName");
                    //jaeger.AgentHost = this.Configuration.GetValue<string>("Jaeger:Host");
                    //jaeger.AgentPort = this.Configuration.GetValue<int>("Jaeger:Port");
                    jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoMvcUi";
                    // When I move to env vars, it'll look something like this:
                    //jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "localhost";
                    jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "host.docker.internal";
                    jaeger.AgentPort = 6831;
                })
                .SetSampler(new AlwaysOnSampler())
                );
            
            /*
            // Add OpenTelemetry Console Exporter & Jaeger Exporter - 1.0.0-rc1.1
            services.AddOpenTelemetryTracing((builder) => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("dotnet-distrubuted-otel-appd.TodoMvcUi"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
                .AddJaegerExporter(jaeger =>
                {
                    jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "host.docker.internal";
                    jaeger.AgentPort = 6831;
                })
                .SetSampler(new AlwaysOnSampler())
                );
            */
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
