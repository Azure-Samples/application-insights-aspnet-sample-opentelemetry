using System.Threading.Channels;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Common;
using Sample.MainApi.HostedServices;

namespace Sample.MainApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpClient();
            services.AddHostedService<HelloHostedService>();
            services.AddSampleAppOptions(Configuration);
            services.AddSingleton<IAppMetrics, Metrics>();
            services.AddSingleton(x => (Metrics)x.GetRequiredService<IAppMetrics>());
            services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            services.AddWebSampleTelemetry(Configuration, (b) =>
            {
                b.AddCollector(t => new RabbitMQCollector.OpenTelemetry.RabbitMQCollector(t));
            });

            var sampleAppOptions = Configuration.GetSampleAppOptions();

            if (sampleAppOptions.UseApplicationInsights)
            {
                services.AddSingleton<ITelemetryModule, RabbitMQCollector.ApplicationInsights.RabbitMQApplicationInsightsModule>();
            }

            // Quick way to create channel
            var channel = Channel.CreateBounded<HelloRequest>(2);
            services.AddSingleton<ChannelReader<HelloRequest>>(channel);
            services.AddSingleton<ChannelWriter<HelloRequest>>(channel);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
