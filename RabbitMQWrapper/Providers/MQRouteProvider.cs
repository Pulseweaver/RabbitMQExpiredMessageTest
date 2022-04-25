namespace RabbitMQWrapper.Services
{
    public class MQRouteProvider
    {
        public MQSettings _Settings;
        public MQRouteProvider(MQSettings settings)
        {
            _Settings = settings;
        }
        public string GetRoute<T>()
        {
            return $"{typeof(T).Name}";
        }
        public string GetDLXRoute<T>()
        {
            return $"{typeof(T).Name}.dlx";
        }
    }
}
