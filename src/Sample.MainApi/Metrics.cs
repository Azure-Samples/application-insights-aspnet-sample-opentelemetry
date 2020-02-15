using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Metrics.Export;
using OpenTelemetry.Trace;
using Sample.Common;

namespace Sample.MainApi
{
    public class Metrics : IAppMetrics
    {
        private readonly Metric appInsightsItemEnqueuedCounter;
        private Meter meter;
        private Counter<long> openTelemetryItemEnqueuedCounter;

        public Metrics(IServiceProvider serviceProvider)
        {
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            if (telemetryClient != null)
            {
                this.appInsightsItemEnqueuedCounter = telemetryClient.GetMetric(new MetricIdentifier("Sample App", "Enqueued Item", "Source"));
            }
        }

        void IAppMetrics.Initialize(MeterFactory meterFactory)
        {
            this.meter = meterFactory.GetMeter("Sample App");
            this.openTelemetryItemEnqueuedCounter = meter.CreateInt64Counter("Enqueued Item");
        }

        public void TrackItemEnqueued(double metricValue, string source)
        {
            appInsightsItemEnqueuedCounter?.TrackValue(metricValue, source);

            if (openTelemetryItemEnqueuedCounter != null)
            {
                var context = default(SpanContext);
                var labelSet = new Dictionary<string, string>() 
                {
                    { "Source", source }
                };
                
                openTelemetryItemEnqueuedCounter.Add(context, 1L, this.meter.GetLabelSet(labelSet));

                // Collect is called here explicitly as there is 
                // no controller implementation yet.
                // TODO: There should be no need to cast to MeterSdk.
                //(meter as MeterSdk).Collect();
            }
        }
    }
}
