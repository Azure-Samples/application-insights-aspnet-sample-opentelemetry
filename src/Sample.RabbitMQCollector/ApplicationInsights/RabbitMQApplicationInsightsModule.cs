using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sample.RabbitMQCollector.ApplicationInsights
{
    public class RabbitMQApplicationInsightsModule : ITelemetryModule, IDisposable
    {
        private RabbitMQCollector collector;
        public RabbitMQApplicationInsightsModule()
        {

        }

        public void Initialize(TelemetryConfiguration configuration)
        {
            if (collector != null)
                return;

            collector = new RabbitMQCollector(new TelemetryClient(configuration));
            collector.Subscribe();
        }

        public void Dispose()
        {
            collector?.Dispose();
        }
    }
}
