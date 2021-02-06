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

            // Updated to use OTLP, otel-collector
            // Adding the OtlpExporter creates a GrpcChannel.
            // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
            // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // TODO: Setup env vars for service.name, service.namespace, e.g.,
            // var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "TodoApi";
            // var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "kjt-Otel-ToDo";
            // ...
            //    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName,serviceNamespace))
            // ...
            
            // Add OpenTelemetry Console Exporter & Jaeger Exporter
            //services.AddOpenTelemetryTracerProvider((builder) => builder
            // 1.0.0-rc1.1
            services.AddOpenTelemetryTracing((builder) => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TodoApi","kjt-Otel-ToDo"))
                .AddSource(nameof(MessageReceiver), nameof(TodoItemsController))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
                //.AddJaegerExporter(jaeger =>
                //{
                    //jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoApi";
                    //jaeger.AgentHost = "host.docker.internal";
                    //jaeger.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOSTNAME") ?? "host.docker.internal";
                    //jaeger.AgentPort = 6831;
                //})
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://host.docker.internal:4317");
                })
                .SetSampler(new AlwaysOnSampler())
                );
            
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
