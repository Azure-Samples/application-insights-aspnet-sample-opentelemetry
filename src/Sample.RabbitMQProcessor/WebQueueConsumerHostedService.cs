using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using Sample.Common;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;
using Sample.RabbitMQCollector;
using System.Collections.Generic;

namespace Sample.RabbitMQProcessor
{
    public class WebQueueConsumerHostedService : IHostedService
    {
        private string rabbitMQHostName;
        private IConnection connection;
        private IModel channel;
        private AsyncEventingBasicConsumer consumer;

        private string timeApiURL;
        private readonly ILogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly Metrics metrics;
        private readonly Tracer tracer;
        private readonly TelemetryClient telemetryClient;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        public WebQueueConsumerHostedService(IOptions<SampleAppOptions> sampleAppOptions,
                                             ILogger<WebQueueConsumerHostedService> logger,
                                             IHttpClientFactory httpClientFactory,
                                             IServiceProvider serviceProvider,
                                             Metrics metrics)
        {
            // To start RabbitMQ on docker:
            // docker run -d --hostname -rabbit --name test-rabbit -p 15672:15672 -p 5672:5672 rabbitmq:3-management
            this.rabbitMQHostName = sampleAppOptions.Value.RabbitMQHostName;

            this.timeApiURL = sampleAppOptions.Value.TimeAPIUrl;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.metrics = metrics;

            // Only using Service Provider because some of the services might not have been registered
            // depending on the choice of the SDK
            var tracerFactory = serviceProvider.GetService<TracerFactoryBase>();
            this.tracer = tracerFactory?.GetApplicationTracer();
            this.telemetryClient = serviceProvider.GetService<TelemetryClient>();
            this.jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var factory = new ConnectionFactory() { HostName = rabbitMQHostName, DispatchConsumersAsync = true };
                    this.connection = factory.CreateConnection();
                    this.channel = connection.CreateModel();

                    channel.QueueDeclare(queue: Constants.WebQueueName, exclusive: false);

                    this.consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.Received += ProcessWebQueueMessageAsync;
                    channel.BasicConsume(queue: Constants.WebQueueName,
                                         autoAck: true,
                                         consumer: consumer);

                    logger.LogInformation("RabbitMQ consumer started, connected to {hostname}", rabbitMQHostName);
                    return;
                }
                catch (BrokerUnreachableException ex)
                {
                    logger.LogError(ex, "Failed to connect to RabbitMQ at {hostname}. Trying again in 3 seconds", rabbitMQHostName);

                    if (this.consumer != null && this.channel != null)
                    {
                        this.channel.BasicCancel(this.consumer.ConsumerTag);                        
                    }

                    this.channel?.Dispose();

                    this.connection?.Dispose();


                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                    }
                }                
            }
        }

        

        private async Task ProcessWebQueueMessageAsync(object sender, BasicDeliverEventArgs @event)
        {
            // ExtractActivity creates the Activity setting the parent based on the RabbitMQ "traceparent" header
            var activity = @event.ExtractActivity("Process single RabbitMQ message");

            ISpan span = null;
            IOperationHolder<DependencyTelemetry> operation = null;
            var processingSucceeded = false;
            string source = string.Empty;

            IDisposable loggingScope = null;
            
            try
            {
                if (tracer != null)
                {
                    // OpenTelemetry seems to require the Activity to have started, unlike AI SDK
                    activity.Start();
                    tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Consumer, out span);

                    span.SetAttribute("queue", Constants.WebQueueName);
                }

                using (operation = telemetryClient?.StartOperation<DependencyTelemetry>(activity))
                {
                    if (operation != null)
                    {
                        operation.Telemetry.Properties.Add("queue", Constants.WebQueueName);
                        operation.Telemetry.Type = ApplicationInformation.Name;
                        operation.Telemetry.Target = this.rabbitMQHostName;
                    }

                    loggingScope = logger.BeginScope("Starting message processing");

                    // Get the payload
                    var message = JsonSerializer.Deserialize<EnqueuedMessage>(@event.Body, jsonSerializerOptions);
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Processing message from {source}: {message}", message.Source, Encoding.UTF8.GetString(@event.Body));
                    }

                    source = message.Source;

                    if ("error".Equals(message.EventName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidEventNameException("Invalid event name");
                    }

                    var apiFullUrl = $"{timeApiURL}/api/time/dbtime";
                    var time = await httpClientFactory.CreateClient().GetStringAsync(apiFullUrl);

                    if (!string.IsNullOrEmpty(message.EventName))
                    {
                        span?.AddEvent(message.EventName);
                        telemetryClient?.TrackEvent(message.EventName);
                    }
                }
                processingSucceeded = true;
            }
            catch (Exception ex)
            {
                if (span != null)
                {
                    span.SetAttribute("error", true);
                    span.Status = Status.Internal.WithDescription(ex.ToString());
                }

                if (operation != null)
                {
                    operation.Telemetry.Success = false;
                    operation.Telemetry.ResultCode = "500";

                    // Track exception, adding the connection to the current activity
                    var exOperation = new ExceptionTelemetry(ex);
                    exOperation.Context.Operation.Id = operation.Telemetry.Context.Operation.Id;
                    exOperation.Context.Operation.ParentId = operation.Telemetry.Context.Operation.ParentId;
                    telemetryClient.TrackException(exOperation);
                }

                logger.LogError(ex, "Failed to process message from {source}", source);
            }
            finally
            {
                span?.End();
                metrics.TrackItemProcessed(1, source, processingSucceeded);
                loggingScope?.Dispose();
            }
        }        
    

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.channel.BasicCancel(this.consumer.ConsumerTag);
            this.channel.Close();
            this.connection.Close();

            return Task.CompletedTask;
        }
    }
}
