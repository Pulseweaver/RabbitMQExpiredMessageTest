using Microsoft.Extensions.Logging;

namespace TestSender
{
    public class ConsoleLogger<T> : ILogger<T>, IDisposable
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {

        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception != null)
                Console.WriteLine($"{state.ToString()} {exception.ToString()}");
            else
                Console.WriteLine(state.ToString());
        }
    }
}
