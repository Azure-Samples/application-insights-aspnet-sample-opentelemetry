using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Sample.Common;
using Microsoft.ApplicationInsights;
using System.Threading.Channels;
using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using System.Linq;

namespace Sample.MainApi.HostedServices
{
    public class HelloHostedService : IHostedService
    {
        private readonly Tracer tracer;
        private readonly TelemetryClient telemetryClient;
        CancellationTokenSource cts;
        Task pendingTask;
        private readonly ChannelReader<HelloRequest> channelReader;

        public HelloHostedService(IServiceProvider serviceProvider, ChannelReader<HelloRequest> channelReader)
        {
            var tracerFactory = serviceProvider.GetService<TracerFactoryBase>();
            this.tracer = tracerFactory?.GetApplicationTracer();

            this.telemetryClient = serviceProvider.GetService<TelemetryClient>();
            cts = new CancellationTokenSource();

            this.channelReader = channelReader;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            pendingTask = Task.Factory.StartNew(() => Processor(cts.Token), TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }

        async Task ProcessItem(HelloRequest request)
        {
            async Task<(Activity Activity, string Message)> OpenTelemetrySayHello(DateTime start, string city)
            {
                var res = await RawSayHello(start, city);
                var span = tracer.StartSpanFromActivity(res.Activity.OperationName, res.Activity, SpanKind.Consumer);

                span.End();

                return res;
            }

            async Task<(Activity Activity, string Message)> ApplicationInsightsSayHello(DateTime start, string city)
            {
                var res = await RawSayHello(start, city);
                var operation = telemetryClient.StartOperation<RequestTelemetry>(res.Activity.OperationName, res.Activity.TraceId.ToString(), res.Activity.SpanId.ToString());

                telemetryClient.StopOperation(operation);

                return res;
            }

            async Task<(Activity Activity, string Message)> RawSayHello(DateTime start, string city)
            {
                var activity = new Activity("Single Say Hello").Start();
                activity.AddBaggage("city", city);
                await Task.Delay(10);
                return (activity, $"{start}: Hello {city}");
            }

            Func<DateTime, string, Task<(Activity Activity, string Message)>> runner = null;

            if (tracer != null)
                runner = OpenTelemetrySayHello;
            else if (telemetryClient != null)
                runner = ApplicationInsightsSayHello;

            if (runner == null)
                return;

            var batchStart = DateTimeOffset.UtcNow;
            var tasks = new List<Task<(Activity Activity, string Message)>>();
            foreach (var v in request.Cities)
            {
                tasks.Add(Task.Run(() => runner(request.RequestTime, v)));
            }


            await Task.WhenAll(tasks);

            if (this.tracer != null)
            {
                var opts = new SpanCreationOptions
                {
                    StartTimestamp = batchStart,
                    Links = tasks.Select(x => new Link(ExtractContext(x.Result.Activity))),
                };

                tracer.StartActiveSpan("Say Hello batch processing", SpanKind.Consumer, opts, out var batchSpan);
                batchSpan.End();
            }
            else if (telemetryClient != null)
            {
                var links = tasks.Select(x => new ApplicationInsightsLink(x.Result.Activity));
                using var batchOperation = telemetryClient.StartOperation<RequestTelemetry>("Say Hello batch processing");
                
                batchOperation.Telemetry.Timestamp = batchStart;

                batchOperation.Telemetry.Properties[ApplicationInsightsLink.TelemetryPropertyName] = System.Text.Json.JsonSerializer.Serialize(links);
            }
        }

        async Task Processor(CancellationToken cancellationToken)
        {
            while (true)
            {
                var req = await channelReader.ReadAsync(cancellationToken);
                await ProcessItem(req);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            cts.Cancel();
            if (pendingTask != null)
                await pendingTask;
        }

        private SpanContext ExtractContext(Activity activity)
        {
            return new SpanContext(activity.TraceId, activity.SpanId, activity.ActivityTraceFlags);
        }
    }
}
