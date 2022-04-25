
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQWrapper;
using Serilog;


var serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
{
    Log.Error(eventArgs.Exception, "FirstChanseException");
};
var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var appSettings = configurationBuilder.Get<AppSettings>();

try
{
    Log.Information("{serviceName} microservice starting up.", serviceName);

    var built = Host.CreateDefaultBuilder(args)
        .UseMQ(context => context.UseSettings(appSettings.MQSettings))
        .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
        .ConfigureServices((hostContext, services) =>
        {
            services
                .AddHostedService<Producer.ConsumerService>()
                .Configure<MQSettings>(configurationBuilder.GetSection("MQSettings"));
        }).Build();
    Log.Information("{serviceName} all built.", serviceName);
    built.Run();
    Log.Information("{serviceName} microservice closing down.", serviceName);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhanled exception in {serviceName}.", serviceName);
}
finally
{
    Log.Information("{serviceName} Cleaning up.", serviceName);
    Log.CloseAndFlush();
    Log.Information("{serviceName} Application Exit.", serviceName);
}
