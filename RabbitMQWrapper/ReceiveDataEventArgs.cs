using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQWrapper
{
    public class ReceivedDataEventArgs<T>
    {
        internal bool Accepted { get; set; }
        public string Sender { get; set; }
        public T Data { get; set; }

        public ReceivedDataEventArgs(T data)
        {
            Data = data;
        }

        public void Accept()
        {
            Accepted = true;
        }
    }
}
