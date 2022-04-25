using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQWrapper.Services;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RabbitMQWrapper.DLX
{
    public class MQDLXConnectorConsumer<T> : ConsumerBase<T>
    {
        private CancellationToken _Token;
        private MQSettings _settings;
        private ILogger<MQDLXConnectorConsumer<T>> _logger;
        private IModel _Channel;
        private IConnection _Connection;
        private readonly MQRouteProvider _routeProvider;
        public Action<DLXMessageWrapper<T>> Received { get; set; }
        

        public MQDLXConnectorConsumer(ILogger<MQDLXConnectorConsumer<T>> logger, MQSettings settings, MQRouteProvider routeProvider)
        {
            _settings = settings;
            _Token = new CancellationToken();
            _logger = logger;
            _routeProvider = routeProvider;
            Init(_Token);
        }

        public void Init(CancellationToken cancellationToken)
        {
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var queueName = _routeProvider.GetDLXRoute<T>();

            _logger.LogTrace("MQDLXConnectorConsumer.Connecting to MQ service {serviceName} at {settingsHostname} with user \"{user}\" and DestinationCode {DestinationCode}",
                serviceName, _settings.Hostname, _settings.Username, _settings.DestinationCode);

            var factory = _settings.Hostname != null ? new ConnectionFactory() { HostName = _settings.Hostname } : new ConnectionFactory();
            factory.ClientProvidedName = "MyAppMQConnectorConsumerFactory";
            factory.AutomaticRecoveryEnabled = true;
            _Connection = factory.CreateConnection();
            if (_Connection != null)
            {
                _Channel = _Connection.CreateModel();

                _Channel.ExchangeDeclare(exchange: _settings.ExchangeDLX, type: _settings.Type);

                var args = new Dictionary<string, object>();
                args.Add("x-dead-letter-exchange", _settings.Exchange);
                args.Add("x-dead-letter-routing-key",  _routeProvider.GetRoute<T>());
                args.Add("x-message-ttl", _settings.DLXTTL);

                try
                {
                    _Channel.QueueDeclare(queue: queueName,
                                            durable: _settings.Durable,
                                            exclusive: _settings.Exclusive,
                                            autoDelete: _settings.AutoDelete,
                                            arguments: args);
                }
                catch(RabbitMQ.Client.Exceptions.OperationInterruptedException)
                {
                    //TODO: Här behöver man troligen hantera fel som tyder på att det inte går att koppla sig till kanalan, radera kanalen?
                    throw;
                }

                _Channel.QueueBind(queue: queueName,
                                    exchange: _settings.ExchangeDLX,
                                    routingKey: queueName);

                _logger.LogTrace("MQDLXConnectorConsumer.Connected to MQ service queue {serviceName}:{queueName} at {host}. ", serviceName, queueName, _settings.Hostname);

                if (_settings.UnitTestMode)
                {
                    //Detta bör inte behövas då vi vill att meddelande ska ligga kvar på DLX tills tiden går ut då de puttas över på original kön.
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

                            var wrapper = GetMessage(ea);
                            Received(wrapper);

                            if (wrapper.Accepted)
                                _Channel.BasicAck(ea.DeliveryTag, false);
                            else
                                _Channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, "Fel i samband med hantering av mottaget meddelande i MQDLXConnectorConsumer.");
                        }
                    };

                    _Channel.BasicConsume(queue: queueName,
                            autoAck: false,
                            consumer: consumer);
                }
            }
        }

        public void Dispose()
        {
            _Channel.Dispose();
            _Connection.Dispose();
        }
    }
}
