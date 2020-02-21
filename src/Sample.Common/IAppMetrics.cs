using OpenTelemetry.Metrics.Configuration;

namespace Sample.Common
{
    public interface IAppMetrics
    {
        void Initialize(MeterFactory meterFactory);
    }
}