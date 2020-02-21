using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Sample.MainApi
{
    public class HelloRequest
    {
        public DateTime RequestTime { get; set; }
        public IEnumerable<string> Cities { get; set; }
        public ActivitySpanId ParentId { get; set; }
        public ActivityTraceId TraceId { get; set; }
    }
}
