using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Sample.RabbitMQCollector.ApplicationInsights
{
    internal class RabbitMQSourceListener : DiagnosticSourceListener
    {
        private readonly TelemetryClient client;

        public RabbitMQSourceListener(TelemetryClient client)
        {
            this.client = client;
        }

        protected override void OnStopActivity(Activity current, object value)
        {
            using var dependency = client.StartOperation<DependencyTelemetry>(current);
            dependency.Telemetry.Type = Constants.ApplicationInsightsTelemetryType;
        }
    }
}
