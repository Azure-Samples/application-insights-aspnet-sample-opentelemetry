using System;
using System.Diagnostics;

namespace Sample.RabbitMQCollector
{
    public class TraceParent
    {
        const string DefaultVersion = "00";
        public const string HeaderKey = "traceparent";

        public ActivityTraceId TraceId { get; }
        public ActivitySpanId SpanId { get; }
        public ActivityTraceFlags Flags { get; }
        public string Version { get; }
        public ActivityTraceFlags TraceFlags { get; }

        public TraceParent(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags flags = ActivityTraceFlags.None, string version = DefaultVersion)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("message", nameof(version));
            }

            TraceId = traceId;
            SpanId = spanId;
            Flags = flags;
            Version = version;
        }

        public override string ToString()
        {
            return string.Join("-", new[] { Version, TraceId.ToString(), SpanId.ToString(), ((int)Flags).ToString("00") });
        }

        public static TraceParent FromCurrentActivity()
        {
            var activity = Activity.Current;
            if (activity == null)
                throw new InvalidOperationException("No current activity");

            return FromActivity(activity);
        }

        public static TraceParent FromActivity(Activity activity)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return new TraceParent(activity.TraceId, activity.SpanId, activity.ActivityTraceFlags, DefaultVersion);
        }

        public static TraceParent CreateFromString(string traceparent)
        {
            if (string.IsNullOrWhiteSpace(traceparent))
            {
                throw new ArgumentException("Invalid traceparent", nameof(traceparent));
            }

            var vals = traceparent.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length != 4)
            {
                throw new ArgumentException("Invalid traceparent format: {traceparent}", traceparent);
            }
            
            var traceId = ActivityTraceId.CreateFromString(vals[1].AsSpan());
            var spanId = ActivitySpanId.CreateFromString(vals[2].AsSpan());
            var flags = vals[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;

            // TODO: validate each item
            return new TraceParent(traceId, spanId, flags, vals[0]);
        }
    }
}
