using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Common
{

    public static class ConfigurationExtensions
    {
        const string SampleAppOptionsConfigSection = "SampleApp";

        public static IServiceCollection AddSampleAppOptions(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<SampleAppOptions>(configuration.GetSection(SampleAppOptionsConfigSection));
        }

        public static SampleAppOptions GetSampleAppOptions(this IConfiguration configuration)
        {
            var telemetryOptions = new SampleAppOptions();
            configuration.GetSection(SampleAppOptionsConfigSection).Bind(telemetryOptions);
            return telemetryOptions;
        }        
    }
}
