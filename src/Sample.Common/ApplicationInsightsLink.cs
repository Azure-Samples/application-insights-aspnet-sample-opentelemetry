using System;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Sample.Common
{
    public class ApplicationInsightsLink
    {
        public const string TelemetryPropertyName = "_MS.links";

        [System.Text.Json.Serialization.JsonPropertyName("operation_Id")]
        public string OperationId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        public ApplicationInsightsLink()
        {
        }

        public ApplicationInsightsLink(Activity activity)
        {
            if (activity is null)
            {
                throw new System.ArgumentNullException(nameof(activity));
            }

            this.OperationId = activity.TraceId.ToString();
            this.Id = activity.Id.ToString();
        }

        public ApplicationInsightsLink(SpanContext spanContext)
        {
            if (!spanContext.IsValid)
            {
                throw new ArgumentException("Invalid span context", nameof(spanContext));
            }

            this.OperationId = spanContext.TraceId.ToString();
            this.Id = spanContext.SpanId.ToString();
        }

        public ApplicationInsightsLink(ActivityTraceId traceId, ActivitySpanId spanId)
        {
            this.OperationId = traceId.ToString();
            this.Id = spanId.ToString();
        }
    }
}
