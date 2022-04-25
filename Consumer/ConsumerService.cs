using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQWrapper;
using RabbitMQWrapper.Interface;

namespace Producer
{
    public class ConsumerService : IHostedService
    {
        private readonly ILogger<ConsumerService> _logger;
        private readonly IMQConsumer<TestMessage> _testMessageMQConsumerService;
        public ConsumerService(ILogger<ConsumerService> logger, IMQConsumer<TestMessage> testMessageMQConsumerService)
        {
            _logger = logger;
            _testMessageMQConsumerService = testMessageMQConsumerService;
            _testMessageMQConsumerService.ReceivedAsync += async (o) => { await TestMessageMQReceived(o); };
            _logger.LogInformation("{serviceName} microservice started.", "SharedTreatment");
        }

        private async Task TestMessageMQReceived(ReceivedDataEventArgs<TestMessage> args)
        {
            var incomingMessage = args.Data; 
            _logger.LogInformation("Received TestMessage : {@incomingMessage}", incomingMessage);
            args.Accept();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
