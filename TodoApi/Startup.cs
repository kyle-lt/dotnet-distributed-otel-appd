using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using TodoApi.Helpers;

// OpenTelemetry Refs
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Samplers;

// Messaging Utils
using Utils.Messaging;

namespace TodoApi
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
            // Add instance of Messaging Utils' MessageReceiver
            services.AddSingleton<MessageReceiver>();

            // This may be changing in the future to:
            /*
            services.AddOpenTelemetryTraceProvider...
            */
            // Add OpenTelemetry Console Exporter & Jaeger Exporter
            services.AddOpenTelemetry((builder) => builder
            .AddActivitySource(nameof(MessageReceiver))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .UseConsoleExporter()
            .UseJaegerExporter(jaeger =>
            {
                jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoApi";
                jaeger.AgentHost = "host.docker.internal";
                jaeger.AgentPort = 6831;
            })
            .SetSampler(new AlwaysOnSampler())
            );

            // RabbitMqReceiver
            services.AddHostedService<RabbitMqReceiver>();

            services.AddControllers();

            // Database Context
            services.AddDbContext<TodoContext>(opt =>
               opt.UseInMemoryDatabase("TodoList"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
