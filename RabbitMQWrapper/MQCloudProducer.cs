using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQWrapper.Interface;
using RabbitMQWrapper.Services;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQWrapper
{
    public class MQCloudProducer : IMQProducer
    {
        private string _serviceName;
        private IModel _channel;
        private IConnection _connection;
        private readonly MQSettings _settings;
        private readonly MQRouteProvider _routeProvider;
        private readonly ILogger<MQCloudProducer> _Logger;

        public MQCloudProducer(ILogger<MQCloudProducer> logger, MQSettings settings, MQRouteProvider routeProvider)
        {
            _settings = settings;
            _routeProvider = routeProvider;
            _Logger = logger;
            _serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            var initResult = Init(logger, settings);
            _connection = initResult.connection;
            _channel = initResult.channel;
        }

        public static (IConnection connection, IModel channel) Init(ILogger logger, MQSettings mqSettings)
        {
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            try
            {
                logger.LogTrace("MQCloudProducer.Producer is connecting to MQ service {_serviceName} at {SettingsHostname} as {Username}", serviceName, mqSettings.Hostname, mqSettings.Username ?? "Username-Missing");

                var factory = new ConnectionFactory()
                {
                    HostName = mqSettings.Hostname,
                    UserName = mqSettings.Username,
                    Password = mqSettings.Password,
                };

                //factory = new ConnectionFactory();
                factory.ClientProvidedName = "MyAppMQProducerFactory";
                factory.AutomaticRecoveryEnabled = true;
                var connection = factory.CreateConnection();
                IModel channel = null;
                if (connection != null)
                {
                    channel = connection.CreateModel();

                    channel.ConfirmSelect();
                    logger.LogTrace("Producer connected to MQ service {_serviceName}.", serviceName);
                }
                else
                    logger.LogError("Producer cannot connect to MQ service {_serviceName}", serviceName);

                return (connection, channel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Producer failed to connect to MQ service {_serviceName}", serviceName);
                throw;
            }
        }

        public bool Produce<T>(T data, string destination)
        {
            return Produce<T>(data, destination, _routeProvider, _channel, _Logger, _settings, _serviceName);
        }

        public static bool Produce<T>(T data, string destination, MQRouteProvider routeProvider, IModel channel, ILogger logger, MQSettings settings, string serviceName)
        {
            if (string.IsNullOrEmpty(destination))
                throw new Exception("Destination is missing a value");

            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            var routingKey = routeProvider.GetRoute<T>();

            if (channel != null)
            {
                channel.QueueBind(routingKey, settings.Exchange, routingKey);

                var basicProperties = channel.CreateBasicProperties();
                basicProperties.Headers = new Dictionary<string, object>();
                basicProperties.Headers.Add("destination", destination);
                basicProperties.UserId = settings.Username;
                basicProperties.Persistent = true;

                logger.LogTrace("Publishing message on queue : {routingKey}", routingKey);
                channel.BasicPublish(exchange: settings.Exchange,
                        routingKey: routingKey,
                        basicProperties: basicProperties,
                        body: body);

                channel.WaitForConfirmsOrDie(new TimeSpan(0, 0, 5));
                logger.LogTrace("Message put on queue : {routingKey}", routingKey);
                return true;
            }
            else
            {
                logger.LogError("{serviceName}: could not connect to MQ channel.", serviceName);
                return false;
            }
        }
        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
