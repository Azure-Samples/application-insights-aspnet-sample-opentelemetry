using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Metrics.Export;

namespace Sample.Common
{
    public class PromotheusExporterHostedService : IHostedService
    {
        private readonly PrometheusExporter exporter;
        private readonly IEnumerable<IAppMetrics> initializers;
        private Timer timer;
        private MeterFactory meterFactory;

        public PromotheusExporterHostedService(PrometheusExporter exporter, IEnumerable<IAppMetrics> initializers)
        {
            this.exporter = exporter ?? throw new System.ArgumentNullException(nameof(exporter));
            this.initializers = initializers;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(5);
            var simpleProcessor = new UngroupedBatcher(exporter, interval);
            this.meterFactory = MeterFactory.Create(simpleProcessor);

            foreach (var initializer in initializers)
            {
                initializer.Initialize(meterFactory);
            }

            this.timer = new Timer(CollectMetrics, meterFactory, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            exporter.Start();

            this.timer.Change(interval, interval);

            return Task.CompletedTask;
        }


        /// <summary>
        /// Need to dig deeper into this
        /// This call should not be needed
        /// </summary>
        /// <param name="state"></param>
        private static void CollectMetrics(object state)
        {
            var meterFactory = (MeterFactory)state;
            var m = meterFactory.GetMeter("Sample App");
            ((MeterSdk)m).Collect();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            exporter.Stop();
            timer.Dispose();
            return Task.CompletedTask;
        }
    }
}