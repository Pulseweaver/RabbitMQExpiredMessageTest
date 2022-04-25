using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQWrapper.DLX;
using RabbitMQWrapper.Interface;
using RabbitMQWrapper.Services;
using System;
using System.Collections.Generic;

namespace RabbitMQWrapper
{
    public static class IHostExtensions
    {
        public static IHostBuilder UseMQ(this IHostBuilder builder, Action<MQCloudContext> context)
        {
            var tmp = new MQCloudContext();
            context?.Invoke(tmp);

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(tmp.Settings ?? new MQSettings());
                services.AddSingleton(typeof(IMQConsumer<>), typeof(MQCloudConsumer<>));
                services.AddSingleton(typeof(MQDLXCloudConsumer<>), typeof(MQDLXCloudConsumer<>));
                services.AddSingleton(typeof(MQDLXConnectorConsumer<>), typeof(MQDLXConnectorConsumer<>));
                services.AddSingleton<IMQProducer, MQCloudProducer>();
                services.AddSingleton<MQRouteProvider>();
            });

            return builder;
        }
    }
    public class MQCloudContext
    {
        internal readonly List<Type> Consumers = new List<Type>();
        internal MQSettings Settings;

        public MQCloudContext()
        {
            //_services = services;
        }

        public MQCloudContext UseSettings(MQSettings settings)
        {
            Settings = settings;
            return this;
        }
    }
}
