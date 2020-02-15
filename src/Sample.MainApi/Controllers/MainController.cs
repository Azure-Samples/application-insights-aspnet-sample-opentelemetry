using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
using Sample.Common;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using System.Threading.Channels;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Sample.MainApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class MainController : ControllerBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string timeApiUrl;
        private readonly ILogger logger;
        private readonly Metrics metrics;
        private readonly ChannelWriter<HelloRequest> channelWriter;
        private readonly Tracer tracer;
        private readonly TelemetryClient telemetryClient;

        public MainController(IOptions<SampleAppOptions> sampleAppOptions,
                              IHttpClientFactory httpClientFactory,
                              ILogger<MainController> logger,
                              IServiceProvider serviceProvider,
                              Metrics metrics,
                              ChannelWriter<HelloRequest> channelWriter)
        {
            this.timeApiUrl = sampleAppOptions.Value.TimeAPIUrl;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.metrics = metrics;
            this.channelWriter = channelWriter;
            var tracerFactory = serviceProvider.GetService<TracerFactoryBase>();
            this.tracer = tracerFactory?.GetApplicationTracer();

            this.telemetryClient = serviceProvider.GetService<TelemetryClient>();
        }
        
        [HttpGet("enqueue/{source}")]
        public async Task<IActionResult> EnqueueAsync(
            [FromServices]IRabbitMQProducer rabbitMQProducer, // Using FromServices to allow lazy creation of RabbitMQ connection
            string source,
            string eventName = null)
        {
            await Task.Delay(100);

            FailGenerator.FailIfNeeded(1);

            var apiFullUrl = $"{timeApiUrl}/api/time/localday";
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Getting time from {url}", apiFullUrl);
            }

            var day = await httpClientFactory.CreateClient().GetStringAsync(apiFullUrl);

            var jsonResponse = new EnqueuedMessage { Day = day, EventName = eventName, Source = source ?? "N/a" };
            var message = System.Text.Json.JsonSerializer.Serialize(jsonResponse);

            rabbitMQProducer.Publish(message);

            metrics.TrackItemEnqueued(1, source);

            return new JsonResult(jsonResponse);
        }


        [HttpGet("dbtime")]
        public async Task<string> GetDbTimeAsync()
        {
            await Task.Delay(100);

            FailGenerator.FailIfNeeded(1);

            var apiFullUrl = $"{timeApiUrl}/api/time/dbtime";
            return await httpClientFactory.CreateClient().GetStringAsync(apiFullUrl);
        }

        [HttpGet("referenceLinks")]
        public async Task<string> ReferenceLinksExample(CancellationToken cancellationToken)
        {
            var req = new HelloRequest
            {
                Cities = new[] { "Zurich", "Seattle", "London" },
                ParentId = Activity.Current.SpanId,
                TraceId = Activity.Current.TraceId,
                RequestTime = DateTime.UtcNow,
            };

            await channelWriter.WriteAsync(req, cancellationToken);

            return $"Queued as {req.RequestTime}";
        }
    }
}
