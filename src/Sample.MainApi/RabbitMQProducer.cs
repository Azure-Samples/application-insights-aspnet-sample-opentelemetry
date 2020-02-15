using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Sample.Common;
using Sample.RabbitMQCollector;

namespace Sample.MainApi
{
    public class RabbitMQProducer : IRabbitMQProducer, IDisposable
    {
        public string HostName { get; private set; }
        public string QueueName { get; private set; }

        private IConnection connection;
        private IModel channel;

        public RabbitMQProducer(IOptions<SampleAppOptions> telemetryOptions)
        {
            HostName = telemetryOptions.Value.RabbitMQHostName;
            QueueName = Constants.WebQueueName;

            this.connection = new ConnectionFactory
            {
                HostName = HostName
            }.CreateConnection();

            this.channel = this.connection.CreateModel().AsActivityEnabled(HostName);
            channel.QueueDeclare(queue: Constants.FirstQueueName, exclusive: false);
        }

        public void Publish(string message)
        {           
            channel.BasicPublish("", QueueName, null, System.Text.Encoding.UTF8.GetBytes(message));
        }

        public void Dispose()
        {
            this.channel?.Close();
            this.connection?.Close();
        }
    }
}
