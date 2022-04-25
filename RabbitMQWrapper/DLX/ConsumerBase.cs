using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQWrapper.DLX
{
    public class ConsumerBase<T>
    {
        public static DLXMessageWrapper<T> GetMessage(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var data = JsonConvert.DeserializeObject<T>(message);

            var reasonBytes = (byte[])ea.BasicProperties.Headers["x-first-death-reason"];
            var destinationBytes = (byte[])ea.BasicProperties.Headers["destination"];
            var str = Encoding.Default.GetString(reasonBytes);
            var destination = Encoding.Default.GetString(destinationBytes);
            var reason = Reason.Other;
            if (str == "expired")
                reason = Reason.Expired;
            else if (str == "rejected")
                reason = Reason.Rejected;
            else if (str == "maxlen")
                reason = Reason.Maxlen;

            var wrapper = new DLXMessageWrapper<T>(data, reason, destination);
            return wrapper;
        }

    }
}
