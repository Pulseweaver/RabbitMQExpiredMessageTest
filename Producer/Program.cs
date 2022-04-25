using Microsoft.Extensions.Configuration;
using RabbitMQWrapper;
using RabbitMQWrapper.Services;
using TestSender;

while (true)
{
    Console.WriteLine("Press S to place treatment on MQ, press E to exit.");
    var input = Console.ReadKey(true).KeyChar.ToString().ToLower();

    if (input == "e")
        return;
    else if (input == "s")
    {
        var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var mqSettings = configurationBuilder.GetSection("MQSettings").Get<MQSettings>();
        var producer = new MQCloudProducer(new ConsoleLogger<MQCloudProducer>(),
                                                    mqSettings,
                                                    new MQRouteProvider(mqSettings));

        producer.Produce<TestMessage>(new TestMessage() { IsSettingTrue = true }, "Producer");
    }
    Console.WriteLine("");
}
