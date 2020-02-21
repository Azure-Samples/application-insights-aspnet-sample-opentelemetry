namespace Sample.MainApi
{
    public interface IRabbitMQProducer
    {
        string HostName { get; }
        string QueueName { get; }

        void Publish(string message);
    }
}
