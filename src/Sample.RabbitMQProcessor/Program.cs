using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Common;

namespace Sample.RabbitMQProcessor
{
    class Program
    {
        public static void Main(string[] args)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
#if DEBUG
                .UseEnvironment("Development")
#endif
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddWorkerSampleTelemetry(hostContext.Configuration);
                    services.AddSingleton<IAppMetrics, Metrics>();
                    services.AddSingleton(x => (Metrics)x.GetRequiredService<IAppMetrics>());

                    services.AddHttpClient();
                    services.AddHostedService<WebQueueConsumerHostedService>();
                    services.AddSampleAppOptions(hostContext.Configuration);
                })
                .ConfigureLogging(SampleServiceCollectionExtensions.ConfigureLogging)
                .ConfigureAppConfiguration((builder) => SampleServiceCollectionExtensions.ConfigureAppConfiguration(builder, args, typeof(Program).Assembly));
        }
    }
}
