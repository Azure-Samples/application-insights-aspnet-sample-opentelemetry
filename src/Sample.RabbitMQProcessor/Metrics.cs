using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Trace;
using Sample.Common;

namespace Sample.RabbitMQProcessor
{
    public class Metrics : IAppMetrics
    {
        private readonly Metric appInsightsProcessedItemCounter;
        private readonly Metric appInsightsProcessedFailedItemCounter;

        private Meter meter;
        private Counter<long> openTelemetryProcessedItemCounter;
        private Counter<long> openTelemetryProcessedFailedItemCounter;

        public Metrics(IServiceProvider serviceProvider)
        {
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            if (telemetryClient != null)
            {
                this.appInsightsProcessedItemCounter = telemetryClient.GetMetric(new MetricIdentifier("Sample App", "Processed Item", "Source"));
                this.appInsightsProcessedFailedItemCounter = telemetryClient.GetMetric(new MetricIdentifier("Sample App", "Processed Failed Item", "Source"));
            }
        }

        void IAppMetrics.Initialize(MeterFactory meterFactory)
        {
            this.meter = meterFactory.GetMeter("Sample App");
            this.openTelemetryProcessedItemCounter = meter.CreateInt64Counter("Processed Item");
            this.openTelemetryProcessedFailedItemCounter = meter.CreateInt64Counter("Processed Failed Item");

        }

        public void TrackItemProcessed(double metricValue, string source, bool succeeded)
        {            
            appInsightsProcessedItemCounter?.TrackValue(succeeded ? 1 : 0, source);
            appInsightsProcessedFailedItemCounter?.TrackValue(succeeded ? 0 : 1, source);

            if (meter != null)
            {
                var context = default(SpanContext);
                var labelSet = new Dictionary<string, string>() 
                {
                    { "Source", source },
                };

                openTelemetryProcessedItemCounter.Add(context, succeeded ? 1 : 0, this.meter.GetLabelSet(labelSet));
                openTelemetryProcessedFailedItemCounter.Add(context, succeeded ? 0 : 1, this.meter.GetLabelSet(labelSet));

                // Collect is called here explicitly as there is 
                // no controller implementation yet.
                // TODO: There should be no need to cast to MeterSdk.
                //(meter as MeterSdk).Collect();
            }
        }
    }
}
