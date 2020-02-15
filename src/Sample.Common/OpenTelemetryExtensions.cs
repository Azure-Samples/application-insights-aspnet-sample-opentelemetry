using System.Reflection;
using OpenTelemetry.Trace;

namespace Sample.Common
{

    public static class OpenTelemetryExtensions
    {        
        public static string TracerServiceName { get; }

        private static readonly string appTracerVersion;

        static OpenTelemetryExtensions()
        {
            TracerServiceName = ApplicationInformation.Name.ToLowerInvariant();
            appTracerVersion = $"semver:{ApplicationInformation.Version.ToString()}";
        }

        public static Tracer GetApplicationTracer(this TracerFactoryBase tracerFactory)
        {
            return tracerFactory.GetTracer(TracerServiceName, appTracerVersion);
        }
    }
}
