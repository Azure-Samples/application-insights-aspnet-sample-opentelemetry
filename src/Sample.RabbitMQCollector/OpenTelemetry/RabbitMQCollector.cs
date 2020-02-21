using System;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;

namespace Sample.RabbitMQCollector.OpenTelemetry
{

    public class RabbitMQCollector : IDisposable
    {
        private readonly Tracer tracer;
        private readonly DiagnosticSourceSubscriber subscriber;


        private static bool DefaultFilter(string activityName, object arg1, object unused)
        {
            return true;
        }

        public void Dispose()
        {
            this.subscriber?.Dispose();
        }

        public RabbitMQCollector(Tracer tracer)
        {
            this.tracer = tracer;
            this.subscriber = new DiagnosticSourceSubscriber(new RabbitMQListener(Constants.DiagnosticsName, tracer), DefaultFilter);
            this.subscriber.Subscribe();
        }
    }
}
