using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Samplers;
using System.Reflection;
using OpenTelemetry.Resources;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using System.IO;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Exporter.Jaeger;
using OpenTelemetry.Exporter.Zipkin;
using Microsoft.Extensions.Options;

namespace Sample.Common
{
    public static class SampleServiceCollectionExtensions
    {
        public static IServiceCollection AddWebSampleTelemetry(this IServiceCollection services, IConfiguration configuration, Action<TracerBuilder> traceBuilder = null)
        {
            var sampleAppOptions = configuration.GetSampleAppOptions();

            if (sampleAppOptions.UseOpenTelemetry)
                services.AddSampleOpenTelemetry(sampleAppOptions, configuration, traceBuilder);

            if (sampleAppOptions.UseApplicationInsights)
                services.AddSampleApplicationInsights(isWeb: true, sampleAppOptions, configuration);

            return services;
        }

        public static IServiceCollection AddWorkerSampleTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var telemetryOptions = configuration.GetSampleAppOptions();

            if (telemetryOptions.UseOpenTelemetry)
                services.AddSampleOpenTelemetry(telemetryOptions, configuration);

            if (telemetryOptions.UseApplicationInsights)
                services.AddSampleApplicationInsights(isWeb: false, telemetryOptions, configuration);

            return services;
        }


        static IServiceCollection AddSampleOpenTelemetry(this IServiceCollection services, SampleAppOptions sampleAppOptions, IConfiguration configuration, Action<TracerBuilder> traceBuilder = null)
        {
            var openTelemetryConfigSection = configuration.GetSection("OpenTelemetry");
            var jaegerConfigSection = openTelemetryConfigSection.GetSection("Jaeger");
            services.Configure<JaegerExporterOptions>(jaegerConfigSection);

            var zipkinConfigSection = openTelemetryConfigSection.GetSection("Zipkin");
            services.Configure<ZipkinTraceExporterOptions>(zipkinConfigSection);            

            // setup open telemetry
            services.AddOpenTelemetry((sp, builder) =>
            {
                var serviceName = OpenTelemetryExtensions.TracerServiceName;

                var exporterCount = 0;

                if (zipkinConfigSection.Exists())
                {
                    var zipkinOptions = sp.GetService<IOptions<ZipkinTraceExporterOptions>>();
                    if (zipkinOptions.Value != null && zipkinOptions.Value.Endpoint != null)
                    {
                        // To start zipkin:
                        // docker run -d -p 9411:9411 openzipkin/zipkin
                        exporterCount++;

                        builder.UseZipkin(o =>
                        {
                            o.Endpoint = zipkinOptions.Value.Endpoint;
                            o.ServiceName = serviceName;
                        });

                        Console.WriteLine("Using OpenTelemetry Zipkin exporter");
                    }
                }
            

                if (!string.IsNullOrWhiteSpace(sampleAppOptions.ApplicationInsightsForOpenTelemetryInstrumentationKey))
                {
                    exporterCount++;

                    builder.UseApplicationInsights(o =>
                    {
                        o.InstrumentationKey = sampleAppOptions.ApplicationInsightsForOpenTelemetryInstrumentationKey;
                        o.TelemetryInitializers.Add(new CloudRoleTelemetryInitializer());
                    });

                    Console.WriteLine("Using OpenTelemetry ApplicationInsights exporter");
                }

                if (jaegerConfigSection.Exists())
                {
                    // Running jaeger with docker
                    // docker run -d --name jaeger \
                    //  -e COLLECTOR_ZIPKIN_HTTP_PORT=19411 \
                    //  -p 5775:5775/udp \
                    //  -p 6831:6831/udp \
                    //  -p 6832:6832/udp \
                    //  -p 5778:5778 \
                    //  -p 16686:16686 \
                    //  -p 14268:14268 \
                    //  -p 19411:19411 \
                    //  jaegertracing/all-in-one
                    var jaegerOptions = sp.GetService<IOptions<JaegerExporterOptions>>();
                    if (jaegerOptions.Value != null && !string.IsNullOrWhiteSpace(jaegerOptions.Value.AgentHost))
                    {
                        exporterCount++;

                        builder.UseJaeger(o =>
                        {
                            o.ServiceName = serviceName;
                            o.AgentHost = jaegerOptions.Value.AgentHost;
                            o.AgentPort = jaegerOptions.Value.AgentPort;
                            o.MaxPacketSize = jaegerOptions.Value.MaxPacketSize;
                            o.ProcessTags = jaegerOptions.Value.ProcessTags;
                        });

                        Console.WriteLine("Using OpenTelemetry Jaeger exporter");
                    }
                }

                if (exporterCount == 0)
                {
                    throw new Exception("No sink for open telemetry was configured");
                }

                builder
                    .SetSampler(new AlwaysSampleSampler())
                    .AddDependencyCollector(config =>
                    {
                        config.SetHttpFlavor = true;
                    })
                    .AddRequestCollector()
                    .SetResource(new Resource(new Dictionary<string, object>
                    {
                        { "service.name", serviceName }
                    }));

                traceBuilder?.Invoke(builder);
            });

            
            var prometheusConfigSection = openTelemetryConfigSection.GetSection("Prometheus");
            if (prometheusConfigSection.Exists())
            {
                var prometheusExporterOptions = new PrometheusExporterOptions();
                prometheusConfigSection.Bind(prometheusExporterOptions);

                if (!string.IsNullOrWhiteSpace(prometheusExporterOptions.Url))
                {
                    var prometheusExporter = new PrometheusExporter(prometheusExporterOptions);
                    services.AddSingleton(prometheusExporter);

                    // Add start/stop lifetime support
                    services.AddHostedService<PromotheusExporterHostedService>();

                    Console.WriteLine($"Using OpenTelemetry Prometheus exporter in '{prometheusExporterOptions.Url}'");
                }
            }

            return services;
        }

        static IServiceCollection AddSampleApplicationInsights(this IServiceCollection services, bool isWeb, SampleAppOptions sampleAppOptions, IConfiguration configuration)
        {
            if (isWeb)
            {
                services.AddApplicationInsightsTelemetry(o =>
                {
                    o.InstrumentationKey = sampleAppOptions.ApplicationInsightsInstrumentationKey;
                    o.ApplicationVersion = ApplicationInformation.Version.ToString();
                });
            }
            else
            {
                services.AddApplicationInsightsTelemetryWorkerService(o =>
                {
                    o.InstrumentationKey = sampleAppOptions.ApplicationInsightsInstrumentationKey;
                    o.ApplicationVersion = ApplicationInformation.Version.ToString();
                });
            }

            services.AddSingleton<ITelemetryInitializer, CloudRoleTelemetryInitializer>();

            Console.WriteLine("Using Application Insights SDK");

            return services;
        }

        public static void ConfigureLogging(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            var telemetryOptions = hostBuilderContext.Configuration.GetSampleAppOptions();

            if (telemetryOptions.UseApplicationInsights)
            {
                loggingBuilder.AddApplicationInsights(telemetryOptions.ApplicationInsightsInstrumentationKey);                
            }

            loggingBuilder.AddConsole((options) => { options.IncludeScopes = true; });
        }

        public static void ConfigureAppConfiguration(IConfigurationBuilder builder, string[] args, Assembly mainAssembly)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json", optional: true);
            builder.AddEnvironmentVariables();

#if DEBUG
            // Needed to add this when using a shared file when debugging
            // It tries to get from the directory where the project is
            //var path = Path.GetDirectoryName(mainAssembly.Location);
            //var envJsonFile = Path.Combine(path, $"appsettings.Development.json");
            builder.AddJsonFile("appsettings.Development.json", optional: true);
#endif
        }
    }
}
