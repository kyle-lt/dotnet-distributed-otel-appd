using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Required for ActivitySource
using System.Diagnostics;

// OpenTelemetry Refs
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace TodoMvcUi
{
    public class Program
    {

        // Create Otel Activity Source
        static readonly ActivitySource activitySource = new ActivitySource(
        "dotnet-distributed-otel-appd.TodoMvcUi");

        public static void Main(string[] args)
        {

            // Configure W3C Context Propagation
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    // For granular logging to the console for Otel o/p
                    logging.AddConsole((options) => { options.IncludeScopes = true; });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://*:60000");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
