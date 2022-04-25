using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQWrapper.Interface;
using RabbitMQWrapper.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper
{
    internal class MQCloudConsumer<T> : IMQConsumer<T>
    {
        private readonly MQSettings _settings;
        private readonly ILogger<MQCloudConsumer<T>> _logger;
        private readonly MQRouteProvider _routeProvider;
        private IModel _channel;
        private IModel _channelDLX;
        private IConnection _Connection;

        private CancellationToken _Token;

        public Action<ReceivedDataEventArgs<T>> Received { get; set; }
        public Func<ReceivedDataEventArgs<T>, Task> ReceivedAsync { get; set; }

        public MQCloudConsumer(ILogger<MQCloudConsumer<T>> logger, MQSettings settings, MQRouteProvider routeProvider)
        {
            _logger = logger;
            _settings = settings;
            _routeProvider = routeProvider;

            _Token = new CancellationToken();
            Init(_Token);
        }

        public void Init(CancellationToken cancellationToken)
        {
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            ConnectionFactory factory = null;
            if (Environment.GetEnvironmentVariable("ENVIRONMENT") is string environment && !string.IsNullOrEmpty(environment))
            {
                _logger.LogTrace("Now running in {environment}.", environment);
                _logger.LogTrace("MQCloudConsumer.Connecting to MQ service {serviceName} at {MQ_URI} with user \"{user}\" and DestinationCode {DestinationCode}",
                    serviceName, Environment.GetEnvironmentVariable("MQ_URI"), _settings.Username, _settings.DestinationCode);

                factory = new ConnectionFactory() { Uri = new Uri(Environment.GetEnvironmentVariable("MQ_URI")) };
            }
            else
            {
                _logger.LogTrace("MQCloudConsumer.Connecting to MQ service {serviceName} at {settingsHostname} with user \"{user}\" and DestinationCode {DestinationCode}",
                serviceName, _settings.Hostname, _settings.Username, _settings.DestinationCode);
                factory = _settings.Hostname != null ? new ConnectionFactory() { HostName = _settings.Hostname, UserName = _settings.Username ?? "Missing-Username", Password = _settings.Password } : new ConnectionFactory();
            }
            factory.ClientProvidedName = "MyAppMQConsumerFactory";
            factory.AutomaticRecoveryEnabled = true;
            factory.DispatchConsumersAsync = true;
            _Connection = factory.CreateConnection();
            if (_Connection != null)
            {
                CreateDeadLetterQueue(_Connection);
                var args = new Dictionary<string, object>();
                args.Add("x-dead-letter-exchange", _settings.ExchangeDLX);
                args.Add("x-dead-letter-routing-key", _routeProvider.GetDLXRoute<T>());
                args.Add("x-message-ttl", _settings.TTL);

                _channel = _Connection.CreateModel();
                _channel.ExchangeDeclare(exchange: _settings.Exchange, type: _settings.Type, durable: true);
                var queueName = _routeProvider.GetRoute<T>();
                try
                {
                    _channel.QueueDeclare(queue: queueName,
                                        durable: _settings.Durable,
                                        exclusive: _settings.Exclusive,
                                        autoDelete: _settings.AutoDelete,
                                        arguments: args);
                }
                catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
                {
                    _logger.LogError(ex, "Kunde inte skapa MQ kanal {channel}. Troligen skiljer sig inställningarna mellan det som skickas in och den kanal som redan finns. Prova radera befintlig kanal om den är tom och kör denna service igen.", queueName);
                    //TODO: Här behöver man troligen hantera fel som tyder på att det inte går att koppla sig till kanalan, radera kanalen?
                    throw;
                }

                _channel.QueueBind(queue: queueName,
                    exchange: _settings.Exchange,
                    routingKey: queueName);

                _logger.LogTrace("MQCloudConsumer.Connected to MQ service queue {serviceName}:{queueName} at {host}. ", serviceName, queueName, _settings.Hostname);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var destination = Encoding.Default.GetString((byte[])ea.BasicProperties.Headers["destination"]);
                        if (Received == null && ReceivedAsync == null || destination?.ToString() != _settings.DestinationCode)
                        {
                            _channel.BasicReject(ea.DeliveryTag, true);
                            return;
                        }

                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;

                        var data = JsonConvert.DeserializeObject<T>(message);
                        if (data != null)
                        {
                            var eventArgs = new ReceivedDataEventArgs<T>(data)
                            {
                                Sender = ea.BasicProperties?.IsUserIdPresent() ?? false
                                    ? ea.BasicProperties.UserId
                                    : null
                            };

                            Received?.Invoke(eventArgs);
                            if (ReceivedAsync != null)
                                await ReceivedAsync(eventArgs);

                            if (eventArgs.Accepted)
                                _channel.BasicAck(ea.DeliveryTag, false);
                            else
                                _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                        else
                            _channel.BasicReject(ea.DeliveryTag, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fel i samband med hantering av mottaget meddelande i MQCloudConsumer.");
                    }
                };

                _channel.BasicConsume(queue: queueName,
                        autoAck: false,
                        consumer: consumer);
            }
        }

        private void CreateDeadLetterQueue(IConnection connection)
        {
            var args = new Dictionary<string, object>();
            args.Add("x-dead-letter-exchange", _settings.Exchange);
            args.Add("x-dead-letter-routing-key", _routeProvider.GetRoute<T>());
            args.Add("x-message-ttl", _settings.DLXTTL);

            _channelDLX = connection.CreateModel();
            _channelDLX.ExchangeDeclare(exchange: _settings.ExchangeDLX, type: _settings.Type);
            var queueName = _routeProvider.GetDLXRoute<T>();
            try
            {
                _channelDLX.QueueDeclare(queue: queueName,
                                        durable: _settings.Durable,
                                        exclusive: _settings.Exclusive,
                                        autoDelete: _settings.AutoDelete,
                                        arguments: args);
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            {
                _logger.LogError(ex, "Kunde inte skapa MQ kanal {channel}. Troligen skiljer sig inställningarna mellan det som skickas in och den kanal som redan finns. Prova radera befintlig kanal om den är tom och kör denna service igen.", queueName);
                //TODO:Går det att koppla sig till kanalen? Kan det vara inställningar som inte matchar? Kanske behövs det någon form av hantering här.
                throw;
            }
            _channelDLX.QueueBind(queue: queueName,
                            exchange: _settings.ExchangeDLX,
                            routingKey: queueName);

            //_channelDLX.Close();
            //_channelDLX.Dispose();
        }

        public void Dispose()
        {
            //Det räcker inte att bara köra Dispose, man måste först köra Close enligt dokumentationen. Det är egentligen inte nödvändigt att stänga channel eftersom den ändå stängs när connection strängs. Det kan dock ses som best practice så därför gör vi det här ändå.
            try { _channel?.Close(); }
            catch (Exception) { }
            finally { _channel?.Dispose(); }

            try { _channelDLX?.Close(); }
            catch (Exception) { }
            finally { _channelDLX?.Dispose(); }

            try { _Connection?.Close(); }
            catch (Exception) { }
            finally { _Connection?.Dispose(); }
        }
    }
}
