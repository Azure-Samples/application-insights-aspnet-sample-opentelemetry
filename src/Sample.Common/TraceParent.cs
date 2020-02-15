//using System;
//using System.Diagnostics;

//namespace Sample.Common
//{
//    public class TraceParent
//    {
//        const string DefaultVersion = "00";
//        public const string HeaderKey = "traceparent";

//        public string TraceId { get; }
//        public string SpanId { get; }
//        public ActivityTraceFlags Flags { get; }
//        public string Version { get; }
//        public ActivityTraceFlags TraceFlags { get; }

//        public TraceParent(string traceId, string spanId, ActivityTraceFlags flags = ActivityTraceFlags.None, string version = DefaultVersion)
//        {
//            if (string.IsNullOrWhiteSpace(traceId))
//            {
//                throw new ArgumentException("message", nameof(traceId));
//            }

//            if (string.IsNullOrWhiteSpace(spanId))
//            {
//                throw new ArgumentException("message", nameof(spanId));
//            }

//            if (string.IsNullOrWhiteSpace(version))
//            {
//                throw new ArgumentException("message", nameof(version));
//            }

//            TraceId = traceId;
//            SpanId = spanId;
//            Flags = flags;
//            Version = version;
//        }

//        public override string ToString()
//        {
//            return string.Join('-', new[] { Version, TraceId, SpanId, ((int)Flags).ToString("00") });
//        }

//        public static TraceParent FromCurrentActivity()
//        {
//            var activity = Activity.Current;
//            if (activity == null)
//                throw new InvalidOperationException("No current activity");

//            return FromActivity(activity);
//        }

//        private static TraceParent FromActivity(Activity activity)
//        {
//            if (activity is null)
//            {
//                throw new ArgumentNullException(nameof(activity));
//            }

//            return new TraceParent(activity.TraceId.ToString(), activity.SpanId.ToString(), activity.ActivityTraceFlags, DefaultVersion);
//        }

//        public static TraceParent CreateFromString(string traceparent)
//        {
//            if (string.IsNullOrWhiteSpace(traceparent))
//            {
//                throw new ArgumentException("Invalid traceparent", nameof(traceparent));
//            }

//            var vals = traceparent.Split('-', StringSplitOptions.RemoveEmptyEntries);
//            if (vals.Length != 4)
//            {
//                throw new ArgumentException("Invalid traceparent format: {traceparent}", traceparent);
//            }

//            // TODO: validate each item
//            var flags = vals[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
//            return new TraceParent(vals[1], vals[2], flags, vals[0]);
//        }
//    }
//}
