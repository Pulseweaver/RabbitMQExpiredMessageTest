using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQWrapper
{
    public class AppSettings
    {
        public MQSettings MQSettings { get; set; }
    }

    public class MQSettings
    {
        public readonly string QueueDown = "queuedown";
        public readonly string QueueDownRoute = "queue.down";
        public readonly string QueueUp = "queueup";
        public readonly string QueueUpRoute = "queue.up";
        public readonly string Exchange = "direct_exchange";
        public readonly string ExchangeDLX = "direct_exchange.dlx";

        public readonly string RoutingKeySharedTreatment = "sharedtreatment";
        public readonly string RoutingKeySystemLog = "systemlog";
        public readonly string RoutingKeyFields = "fields";

        public readonly string Type = "direct";
        public readonly bool Durable = true;
        public readonly bool Exclusive = false;
        public readonly bool AutoDelete = false;
        public readonly int TTL = 8000;
        public readonly int DLXTTL = 8000;

        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VHost { get; set; }
        public string DestinationCode { get; set; }

        public bool UnitTestMode { get; set; } = false;

        public MQSettings()
        {

        }

        public MQSettings(string hostname = null, int ttl = 60000, string destinationCode = null, bool unitTestMode = false)
        {
            Hostname = hostname;
            TTL = ttl;
            DestinationCode = destinationCode;
            UnitTestMode = unitTestMode;
        }
    }
}
