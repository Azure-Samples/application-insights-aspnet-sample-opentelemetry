using System;
using System.Diagnostics;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;

namespace Sample.RabbitMQCollector.OpenTelemetry
{
    public class RabbitMQListener : ListenerHandler
    {
        public RabbitMQListener(string sourceName, Tracer tracer) : base(sourceName, tracer)
        {
        }

        public override void OnStartActivity(Activity activity, object payload)
        {
            var span = this.Tracer.StartSpanFromActivity(activity.OperationName, activity);
            foreach (var kv in activity.Tags)
            {
                span.SetAttribute(kv.Key, kv.Value);
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            var span = this.Tracer.CurrentSpan;
            span.End();
            if (span is IDisposable disposableSpan)
            {
                disposableSpan.Dispose();
            }
        }
    }
}
