using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

// RabbitMQ
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TodoApi.Helpers
{
    public class RabbitMqReceiver : BackgroundService
    {
        private readonly ILogger<RabbitMqReceiver> _logger;
        private IConnection _connection;  
        private IModel _channel;

        public RabbitMqReceiver(ILogger<RabbitMqReceiver> logger)
        {
            _logger = logger;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            _logger.LogInformation("Initializing RabbitMqReceiver!");
            
            var factory = new ConnectionFactory() { HostName = "rabbitmq" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "hello",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Executing RabbitMqReceiver ExecuteAsync!");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
                _logger.LogInformation("Received Message Successfully!");
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
        // Stubs
        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)  {  }  
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) {  }  
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)  {  }  
    
        public override void Dispose()  
        {  
            _channel.Close();  
            _connection.Close();  
            base.Dispose();  
        }
    }
}