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

// TodoApi Refs
using TodoApi.Models;
using TodoApi.Helpers;
using TodoApi.Controllers;

// OpenTelemetry Refs
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
            
            // Add OpenTelemetry Console Exporter & Jaeger Exporter
            services.AddOpenTelemetryTracerProvider((builder) => builder
            .AddSource(nameof(MessageReceiver), nameof(TodoItemsController))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddJaegerExporter(jaeger =>
            {
                jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoApi";
                //jaeger.AgentHost = "host.docker.internal";
                jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "host.docker.internal";
                jaeger.AgentPort = 6831;
            })
            .SetSampler(new AlwaysOnSampler())
            );
            
            /*
            // Add OpenTelemetry Console Exporter & Jaeger Exporter - 1.0.0-rc1.1
            services.AddOpenTelemetryTracing((builder) => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("dotnet-distrubuted-otel-appd.TodoApi"))
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
            
            services.AddControllers();

            // Database Context for In-Memory DB
            //services.AddDbContext<TodoContext>(opt =>
            //   opt.UseInMemoryDatabase("TodoList"));

            // Database Context for SQLite DB
            services.AddDbContext<TodoContext>(opt =>
               opt.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            // Add instance of Messaging Utils' MessageReceiver
            services.AddSingleton<MessageReceiver>();

            // RabbitMqReceiver
            services.AddHostedService<RabbitMqReceiver>();
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
