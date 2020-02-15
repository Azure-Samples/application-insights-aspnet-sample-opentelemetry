using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Common;
using Sample.TimeApi.Data;

namespace Sample.TimeApi
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
            services.AddSingleton<IDeviceRepository, SqlDeviceRepository>();
            services.AddSampleAppOptions(Configuration);
            services.AddWebSampleTelemetry(Configuration);

            var sampleAppOptions = Configuration.GetSampleAppOptions();

            if (sampleAppOptions.UseOpenTelemetry)
            {
                services.AddSingleton<SqlDeviceRepository>();
                services.AddSingleton<IDeviceRepository, OpenTelemetryCollectingDeviceRepository<SqlDeviceRepository>>();
            }
            else
            {
                services.AddSingleton<IDeviceRepository, SqlDeviceRepository>();
            }
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
