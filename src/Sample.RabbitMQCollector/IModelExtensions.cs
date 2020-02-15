using Sample.RabbitMQCollector;

namespace RabbitMQ.Client
{
    public static class IModelExtensions
    {
        public static IModel AsActivityEnabled(this IModel model, string hostname)
        {
            if (model == null)            
                return null;            

            if (string.IsNullOrWhiteSpace(hostname))            
                throw new System.ArgumentException("Missing hostname", nameof(hostname));            

            return new ActivityEnabledModel(model, hostname);
        }
    }
}
