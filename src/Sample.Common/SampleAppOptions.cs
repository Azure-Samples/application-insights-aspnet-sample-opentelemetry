namespace Sample.Common
{
    public class SampleAppOptions
    {
        public string RabbitMQHostName { get; set; } = "localhost";
        public string TimeAPIUrl { get; set; } = "http://localhost:5002";
        public bool UseOpenTelemetry { get; set; }
        public bool UseApplicationInsights { get; set; }

        public string ApplicationInsightsInstrumentationKey { get; set; }
        public string ApplicationInsightsForOpenTelemetryInstrumentationKey { get; set; }
    }
}
