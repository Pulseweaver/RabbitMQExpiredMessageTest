using Microsoft.Extensions.Logging;
using RabbitMQWrapper.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper.DLX
{
    public class MQDLXCloudConsumer<T> : ConsumerBase<T>
    {
        private CancellationToken _Token;
        private MQSettings _Settings;
        private ILogger<MQDLXCloudConsumer<T>> _logger;
        private IModel _Channel;
        private IConnection _Connection;
        private readonly MQRouteProvider _routeProvider;
        public Action<DLXMessageWrapper<T>> Received { get; set; }

        public MQDLXCloudConsumer(ILogger<MQDLXCloudConsumer<T>> logger, MQSettings settings, MQRouteProvider routeProvider)
        {
            _Settings = settings;
            _Token = new CancellationToken();
            _logger = logger;
            _routeProvider = routeProvider;
            Init(_Token);
        }

        public void Init(CancellationToken cancellationToken)
        {
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var queueName = _routeProvider.GetDLXRoute<T>();
            var exchange = _Settings.ExchangeDLX;

            _logger.LogTrace("MQDLXCloudConsumer.Connecting to MQ service {serviceName} at {settingsHostname} with user \"{user}\" and DestinationCode {DestinationCode}",
                serviceName, _Settings.Hostname, _Settings.Username, _Settings.DestinationCode);

            var factory = _Settings.Hostname != null ? new ConnectionFactory() { HostName = _Settings.Hostname } : new ConnectionFactory();
            factory.ClientProvidedName = "MyAppMQDLXConsumerFactory";
            factory.AutomaticRecoveryEnabled = true;
            _Connection = factory.CreateConnection();
            if (_Connection != null)
            {
                _Channel = _Connection.CreateModel();

                _Channel.ExchangeDeclare(exchange: exchange, type: _Settings.Type);
                _Channel.QueueDeclare(queue: queueName,
                                        durable: _Settings.Durable,
                                        exclusive: _Settings.Exclusive,
                                        autoDelete: _Settings.AutoDelete,
                                        arguments: null);

                _Channel.QueueBind(queue: queueName,
                    exchange: exchange,
                    routingKey: queueName);

                _logger.LogTrace("MQDLXCloudConsumer.Connected to MQ service queue {serviceName}:{queueName} at {host}. ", serviceName, queueName, _Settings.Hostname);

                var consumer = new EventingBasicConsumer(_Channel);

                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        if (Received == null)
                        {
                            _Channel.BasicReject(ea.DeliveryTag, true);
                            return;
                        }

                        var msg = GetMessage(ea);
                        Received(msg);

                        if (msg.Accepted)
                            _Channel.BasicAck(ea.DeliveryTag, false);
                        else
                            _Channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fel i samband med hantering av mottaget meddelande i MQDLXCloudConsumer.");
                    }
                };

                _Channel.BasicConsume(queue: queueName,
                        autoAck: false,
                        consumer: consumer);
            }
        }

        public void Dispose()
        {
            _Channel.Dispose();
            _Connection.Dispose();
        }
    }
}