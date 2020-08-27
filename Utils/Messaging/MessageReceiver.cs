using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Utils.Messaging
{
    public class MessageReceiver : IDisposable
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(MessageReceiver));
        private static readonly ITextFormat TextFormat = new TraceContextFormat();

        private readonly ILogger<MessageReceiver> logger;
        private readonly IConnection connection;
        private readonly IModel channel;

        public MessageReceiver(ILogger<MessageReceiver> logger)
        {
            this.logger = logger;
            
            this.logger.LogInformation("Waiting 10 seconds for RabbitMQ to boot...");
            //Task.Delay(5000).Wait();
            Thread.Sleep(10000);
            this.logger.LogInformation("10 seconds elapsed, initializing RabbitMqReceiver!");

            this.connection = RabbitMqHelper.CreateConnection();
            this.channel = RabbitMqHelper.CreateModelAndDeclareTestQueue(this.connection);
        }

        public void Dispose()
        {
            this.channel.Dispose();
            this.connection.Dispose();
        }

        public void StartConsumer()
        {
            RabbitMqHelper.StartConsumer(this.channel, this.ReceiveMessage);
        }

        public void ReceiveMessage(BasicDeliverEventArgs ea)
        {
            // Extract the ActivityContext of the upstream parent from the message headers.
            //var parentContext = TextFormat.Extract(default, ea.BasicProperties, this.ExtractTraceContextFromBasicProperties);
            /* ktully
                * Back-porting my code to support latest nuget 0.4.0-beta2
                * The above will be the way to do it upon next release!!
            */
            var parentContext = TextFormat.Extract(ea.BasicProperties, ExtractTraceContextFromBasicProperties);

            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{ea.RoutingKey} receive";

            //using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext))
            /* ktully
                * Back-porting my code to support latest nuget 0.4.0-beta2
                * The above will be the way to do it upon next release!!
            */
            using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext))
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.Span.ToArray());

                    this.logger.LogInformation($"Message received: [{message}]");

                    if (activity != null)
                    {
                        activity.AddTag("message", message);

                        // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                        RabbitMqHelper.AddMessagingTags(activity);
                    }

                    // Simulate some work
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Message processing failed.");
                }
            }
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
        {
            try
            {
                if (props.Headers.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to extract trace context: {ex}");
            }

            return Enumerable.Empty<string>();
        }
    }
}