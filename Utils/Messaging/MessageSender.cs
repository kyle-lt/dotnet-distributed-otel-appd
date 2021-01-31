using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace Utils.Messaging
{
    public class MessageSender : IDisposable
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(MessageSender));
        
        // Changed from ITextFormat to IPropagator for 0.4.0-beta2 to 0.5.0-beta2
        //private static readonly IPropagator Propagator = new TextMapPropagator();
        //private static readonly ITextFormat TextFormat = new TraceContextFormat();
        // 1.0.0-rc1.1
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        
        // 0.8.0-beta.1 & 1.0.0-rc1.1 is supposed to change IPropagator to TraceContextPropagator...not working.
        // This should work, it doesn't - https://github.com/open-telemetry/opentelemetry-dotnet/blob/master/src/OpenTelemetry.Api/Context/Propagation/Propagators.cs
        //private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        // This should also work, it doesn't: https://github.com/open-telemetry/opentelemetry-dotnet/blob/master/src/OpenTelemetry.Api/Context/Propagation/TraceContextPropagator.cs
        //private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

        private readonly ILogger<MessageSender> logger;
        private readonly IConnection connection;
        private readonly IModel channel;

        public MessageSender(ILogger<MessageSender> logger)
        {
            this.logger = logger;
            this.connection = RabbitMqHelper.CreateConnection();
            this.channel = RabbitMqHelper.CreateModelAndDeclareTestQueue(this.connection);
        }

        public void Dispose()
        {
            this.channel.Dispose();
            this.connection.Dispose();
        }

        public string SendMessage()
        {
            try
            {
                // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
                // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
                var activityName = $"{RabbitMqHelper.TestQueueName} send";

                using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer))
                {
                    var props = this.channel.CreateBasicProperties();

                    // Depending on Sampling (and whether a listener is registered or not), the
                    // activity above may not be created.
                    // If it is created, then propagate its context.
                    // If it is not created, the propagate the Current context,
                    // if any.
                    ActivityContext contextToInject = default;
                    if (activity != null)
                    {
                        this.logger.LogInformation("Injecting Context From RabbitMqHelper Activity");
                        contextToInject = activity.Context;
                    }
                    else if (Activity.Current != null)
                    {
                        this.logger.LogInformation("Injecting Context From Some Existing Activity");
                        contextToInject = Activity.Current.Context;
                    }

                    // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                    //Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, this.InjectTraceContextIntoBasicProperties);
                    Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props, this.InjectTraceContextIntoBasicProperties);
                    /* ktully
                        * The code below is a back-port to support nuget 0.4.0-beta2
                        * The above code is the way to do it for the currentrelease (0.5.0-beta2)!!
                    */
                    //TextFormat.Inject(activity.Context, props, this.InjectTraceContextIntoBasicProperties);
                
                    // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                    RabbitMqHelper.AddMessagingTags(activity);
                    var body = $"Published message: DateTime.Now = {DateTime.Now}.";

                    this.channel.BasicPublish(
                        exchange: RabbitMqHelper.DefaultExchangeName,
                        routingKey: RabbitMqHelper.TestQueueName,
                        basicProperties: props,
                        body: Encoding.UTF8.GetBytes(body));

                    this.logger.LogInformation($"Message sent: [{body}]");

                    return body;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Message publishing failed.");
                throw;
            }
        }

        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
            try
            {
                if (props.Headers == null)
                {
                    props.Headers = new Dictionary<string, object>();
                }
                this.logger.LogInformation("Injecting Context using Queue IBasicProperties");
                this.logger.LogInformation("key = " + key);
                this.logger.LogInformation("value = " + value);
                props.Headers[key] = value;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to inject trace context.");
            }
        }
    }
}