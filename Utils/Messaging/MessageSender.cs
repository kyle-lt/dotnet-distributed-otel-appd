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
        private static readonly IPropagator Propagator = new TextMapPropagator();
        //private static readonly ITextFormat TextFormat = new TraceContextFormat(); 

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

                    if (activity != null)
                    {
                        // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                        //TextFormat.Inject(new PropagationContext(activity.Context, activity.Baggage), props, this.InjectTraceContextIntoBasicProperties);
                        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, this.InjectTraceContextIntoBasicProperties);
                        /* ktully
                            * The code below is a back-port to support nuget 0.4.0-beta2
                            * The above code is the way to do it for the currentrelease (0.5.0-beta2)!!
                        */
                        //TextFormat.Inject(activity.Context, props, this.InjectTraceContextIntoBasicProperties);
                    
                        // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                        RabbitMqHelper.AddMessagingTags(activity);
                    }

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

                props.Headers[key] = value;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to inject trace context.");
            }
        }
    }
}