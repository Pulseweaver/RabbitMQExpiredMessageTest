using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQWrapper.DLX
{
    public class DLXMessageWrapper<T>
    {
        public T Data { get; set; }
        public Reason Reason { get; set; }
        public string Destination { get; set; }
        public bool Accepted { get; set; } = false;

        public DLXMessageWrapper(T data, Reason reason, string destination)
        {
            Data = data;
            Reason = reason;
            Destination = destination;
        }
    }

    public enum Reason
    {
        Rejected,
        Expired,
        Maxlen,
        Other
    }
}
