using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Sample.RabbitMQCollector
{
    /// <summary>
    /// Wrapper for <see cref="IModel"/>, publishing <see cref="System.Diagnostics.Activity"/>
    /// For the simplicity purpose only <see cref="BasicPublish"/> has activity publishing
    /// </summary>
    public sealed class ActivityEnabledModel : IModel
    {
        static DiagnosticSource diagnosticSource = new DiagnosticListener(Constants.DiagnosticsName);

        private readonly IModel model;
        private readonly string hostname;

        public ActivityEnabledModel(IModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ActivityEnabledModel(IModel model, string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentException("message", nameof(hostname));
            }

            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.hostname = hostname;
        }

        public int ChannelNumber => model.ChannelNumber;

        public ShutdownEventArgs CloseReason => model.CloseReason;

        public IBasicConsumer DefaultConsumer
        {
            get => model.DefaultConsumer;
            set => model.DefaultConsumer = value;
        }

        public bool IsClosed => model.IsClosed;

        public bool IsOpen => model.IsOpen;

        public ulong NextPublishSeqNo => model.NextPublishSeqNo;

        public TimeSpan ContinuationTimeout
        {
            get => model.ContinuationTimeout;
            set => model.ContinuationTimeout = value;
        }

        public event EventHandler<BasicAckEventArgs> BasicAcks
        {
            add => model.BasicAcks += value;
            remove => model.BasicAcks -= value;
        }

        public event EventHandler<BasicNackEventArgs> BasicNacks
        {
            add => model.BasicNacks += value;
            remove => model.BasicNacks -= value;
        }

        public event EventHandler<EventArgs> BasicRecoverOk
        {
            add => model.BasicRecoverOk += value;
            remove => model.BasicRecoverOk -= value;
        }
        
        public event EventHandler<BasicReturnEventArgs> BasicReturn
        {
            add => model.BasicReturn += value;
            remove => model.BasicReturn -= value;
        }

        public event EventHandler<CallbackExceptionEventArgs> CallbackException
        {
            add => model.CallbackException += value;
            remove => model.CallbackException -= value;
        }
        
        public event EventHandler<FlowControlEventArgs> FlowControl
        {
            add => model.FlowControl += value;
            remove => model.FlowControl -= value;
        }
        
        public event EventHandler<ShutdownEventArgs> ModelShutdown
        {
            add => model.ModelShutdown += value;
            remove => model.ModelShutdown -= value;
        }

        public void Abort() => model.Abort();

        public void Abort(ushort replyCode, string replyText) => model.Abort(replyCode, replyText);

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            model.BasicAck(deliveryTag, multiple);
        }

        public void BasicCancel(string consumerTag)
        {
            model.BasicCancel(consumerTag);
        }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
        }

        public BasicGetResult BasicGet(string queue, bool autoAck)
        {
            return model.BasicGet(queue, autoAck);
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            model.BasicNack(deliveryTag, multiple, requeue);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
        {
            Activity activity = null;
            if (diagnosticSource.IsEnabled(Constants.DiagnosticsName))
            {
                activity = new Activity(Constants.PublishActivityName);
                activity.AddTag(Constants.OperationTagName, Constants.PublishOperation);
                activity.AddTag(Constants.MessageSizeTagName, (body?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
                if (this.hostname != null)
                    activity.AddTag(Constants.HostTagName, this.hostname);

                if (!string.IsNullOrWhiteSpace(exchange))
                    activity.AddTag(Constants.ExchangeTagName, exchange);

                if (!string.IsNullOrWhiteSpace(routingKey))
                    activity.AddTag(Constants.RoutingKeyTagName, routingKey);

                diagnosticSource.StartActivity(activity, null);
            }

            // Add into the header the current activity identifier
            basicProperties = basicProperties ?? model.CreateBasicProperties();
            if (basicProperties.Headers == null)
            {
                basicProperties.Headers = new Dictionary<string, object>();
            }

            basicProperties.Headers.Add(TraceParent.HeaderKey, Activity.Current.Id);

            try
            {
                model.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
            }
            finally
            {
                if (activity != null)
                {
                    diagnosticSource.StopActivity(activity, null);
                }
            }
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            model.BasicQos(prefetchSize, prefetchCount, global);
        }

        public void BasicRecover(bool requeue)
        {
            model.BasicRecover(requeue);
        }

        public void BasicRecoverAsync(bool requeue)
        {
            model.BasicRecoverAsync(requeue);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            model.BasicReject(deliveryTag, requeue);
        }

        public void Close()
        {
            model.Close();
        }

        public void Close(ushort replyCode, string replyText)
        {
            model.Close(replyCode, replyText);
        }

        public void ConfirmSelect()
        {
            model.ConfirmSelect();
        }

        public uint ConsumerCount(string queue)
        {
            return model.ConsumerCount(queue);
        }

        public IBasicProperties CreateBasicProperties()
        {
            return model.CreateBasicProperties();
        }

        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            return model.CreateBasicPublishBatch();
        }

        public void Dispose()
        {
            model.Dispose();
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            model.ExchangeBind(destination, source, routingKey, arguments);
        }

        public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            model.ExchangeBindNoWait(destination, source, routingKey, arguments);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            model.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
        }

        public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            model.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            model.ExchangeDeclarePassive(exchange);
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            model.ExchangeDelete(exchange, ifUnused);
        }

        public void ExchangeDeleteNoWait(string exchange, bool ifUnused)
        {
            model.ExchangeDeleteNoWait(exchange, ifUnused);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            model.ExchangeUnbind(destination, source, routingKey, arguments);
        }

        public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            model.ExchangeUnbindNoWait(destination, source, routingKey, arguments);
        }

        public uint MessageCount(string queue)
        {
            return model.MessageCount(queue);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            model.QueueBind(queue, exchange, routingKey, arguments);
        }

        public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            model.QueueBindNoWait(queue, exchange, routingKey, arguments);
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            return model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            model.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return model.QueueDeclarePassive(queue);
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            return model.QueueDelete(queue, ifUnused, ifEmpty);            
        }

        public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
        {
            model.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
        }

        public uint QueuePurge(string queue)
        {
            return model.QueuePurge(queue);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            model.QueueUnbind(queue, exchange, routingKey, arguments);
        }

        public void TxCommit()
        {
            model.TxCommit();
        }

        public void TxRollback()
        {
            model.TxRollback();
        }

        public void TxSelect()
        {
            model.TxSelect();
        }

        public bool WaitForConfirms()
        {
            return model.WaitForConfirms();
        }

        public bool WaitForConfirms(TimeSpan timeout)
        {
            return model.WaitForConfirms(timeout);
        }

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            return WaitForConfirms(timeout, out timedOut);
        }

        public void WaitForConfirmsOrDie()
        {
            model.WaitForConfirmsOrDie();
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            model.WaitForConfirmsOrDie(timeout);
        }
    }
}
