using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

// RabbitMQ
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// OpenTelemetry
using OpenTelemetry;
using OpenTelemetry.Trace;

using Utils.Messaging;

namespace TodoApi.Helpers
{
    public class RabbitMqReceiver : BackgroundService
    {
        private readonly ILogger<RabbitMqReceiver> _logger;
        //private IConnection _connection;  
        //private IModel _channel;
        private readonly MessageReceiver _messageReceiver;
        
        // Create ActivitySource to capture my manual Spans - this ActivitySource is Added to the OpenTelemetry
        // Service declaration in Startup.cs
        //private static readonly ActivitySource _activitySource = new ActivitySource("ManualActivitySource");

        public RabbitMqReceiver(ILogger<RabbitMqReceiver> logger, MessageReceiver messageReceiver)
        {
            _logger = logger;
            _messageReceiver = messageReceiver;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _messageReceiver.StartConsumer();

            await Task.CompletedTask;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }

        /*
        private void InitRabbitMQ()
        {
            _logger.LogInformation("Waiting 5 seconds for RabbitMQ to boot...");
            Task.Delay(5000).Wait();
            _logger.LogInformation("5 seconds elapsed, initializing RabbitMqReceiver!");
            
            var factory = new ConnectionFactory() { HostName = "host.docker.internal" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "hello",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }
        */

        /* OLD - replaced by Utils.Messaging
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Executing RabbitMqReceiver ExecuteAsync!");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                String traceparent = "";
                IBasicProperties props = ea.BasicProperties;
                if (props.Headers.TryGetValue("traceparent", out var rawTraceParent) && rawTraceParent is byte[] binRawTraceParent)
                {
                    traceparent = Encoding.UTF8.GetString(binRawTraceParent);
                    _logger.LogInformation($"traceparent header from rabbitmq = {traceparent}");
                }
                else {
                    _logger.LogInformation($"traceparent header from rabbitmq NOT FOUND!");
                }

                // Manually create Trace Provider using SDK - I am trying this becuase the dependency injection method
                // isn't grabbing my RabbitMQ Consumer traces...
                // Note, the syntax for this may be changing in the future to something more like
                
                //using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                //    .AddSource("MyCompany.MyProduct.MyLibrary")
                //    .AddConsoleExporter()
                //    .Build();
                
                using var tracerProvider = Sdk.CreateTracerProvider(builder => builder
                    .AddActivitySource("ManualActivitySource")
                    .UseConsoleExporter()
                    .UseJaegerExporter(jaeger =>
                    {
                        jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoApi";
                        jaeger.AgentHost = "host.docker.internal";
                        jaeger.AgentPort = 6831;
                    })
                );
                

                // Consume and process a message from RabbitMQ, and wrap it in a Span, initializing it with the Message Producer's traceparent header
                using (var activity = _activitySource.StartActivity("RabbitMQ Consumer", ActivityKind.Consumer).SetParentId(traceparent))
                {
                    if (activity?.IsAllDataRequested ?? false)
                    {
                        // Set ParentId
                        //activity?.SetParentId(traceparent);
                        
                        // Adding Tags and Events to new Child Activity
                        activity?.AddTag("rabbit.consumer.tag.1", "Is it working?");
                        activity?.AddTag("rabbit.consumer.tag.2", "Yes");
                        activity?.AddEvent(new ActivityEvent("This is the event body - kinda equivalent to a log entry."));

                        // Debug Logging
                        _logger.LogInformation("----- Begin logging new Activity Props -----");
                        _logger.LogInformation($"Activity.Current.TraceId = {Activity.Current.TraceId}");
                        _logger.LogInformation($"Activity.Current.SpanId = {Activity.Current.SpanId}");
                        _logger.LogInformation($"Activity.Current.ParentId = {Activity.Current.ParentId}");
                        _logger.LogInformation("----- Done Logging new Activity Props -----");
                        
                        // Simulate Work Being Done
                        Task.Delay(2000).Wait();
                        _logger.LogInformation($"Received Message Successfully! Message Body = {message}");
                    }
                }
            };

            // Do I need this block?
            consumer.Shutdown += OnConsumerShutdown;  
            consumer.Registered += OnConsumerRegistered;  
            consumer.Unregistered += OnConsumerUnregistered;  
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume(queue: "hello",
                                autoAck: true,
                                consumer: consumer);

            return Task.CompletedTask;

        }
        */
        // Stubs
        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)  {  }  
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) {  }  
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)  {  }  
    
        public override void Dispose()  
        {  
            //_channel.Close();  
            //_connection.Close();  
            base.Dispose();  
        }
    }
}