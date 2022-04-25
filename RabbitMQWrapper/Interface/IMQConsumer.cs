using System;
using System.Threading.Tasks;

namespace RabbitMQWrapper.Interface
{
    public interface IMQConsumer<T> : IDisposable
    {
        Action<ReceivedDataEventArgs<T>> Received { get; set; }
        Func<ReceivedDataEventArgs<T>, Task> ReceivedAsync { get; set; }
    }
}
