using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IdentityMapping.Infrastructure.Messaging
{
    public class RabbitMQMessageBroker : IMessageBroker, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQMessageBroker> _logger;
        private readonly RabbitMQSettings _settings;
        private bool _disposed;

        public RabbitMQMessageBroker(
            IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQMessageBroker> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    Port = _settings.Port
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare the default exchange
                _channel.ExchangeDeclare(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("Connected to RabbitMQ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
        }

        public Task PublishAsync<T>(T message, string routingKey) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.Type = typeof(T).Name;

                _channel.BasicPublish(
                    exchange: _settings.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation($"Published message of type {typeof(T).Name} to {routingKey}");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing message to {routingKey}");
                throw;
            }
        }

        public Task SubscribeAsync<T>(string queueName, Func<T, Task> handler) where T : class
        {
            try
            {
                // Declare a queue
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Bind the queue to the exchange with the queue name as routing key
                _channel.QueueBind(
                    queue: queueName,
                    exchange: _settings.ExchangeName,
                    routingKey: queueName);

                // Set prefetch count
                _channel.BasicQos(prefetchSize: 0, prefetchCount: _settings.PrefetchCount, global: false);

                // Create a consumer
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(body));

                        if (message != null)
                        {
                            await handler(message);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            _logger.LogInformation($"Processed message of type {typeof(T).Name} from {queueName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing message from {queueName}");
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                // Start consuming
                _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation($"Subscribed to queue {queueName}");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error subscribing to queue {queueName}");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }

            _disposed = true;
        }
    }
} 