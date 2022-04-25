using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQWrapper.Interface
{
    public interface IMQProducer
    {
        public bool Produce<T>(T data, string destinationCode);
    }
}
