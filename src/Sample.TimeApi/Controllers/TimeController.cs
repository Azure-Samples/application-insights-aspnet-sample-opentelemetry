using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
using Sample.Common;
using Microsoft.Extensions.Logging;
using Sample.TimeApi.Data;
using System.Text;

namespace Sample.TimeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeController : ControllerBase
    {
        private readonly IDeviceRepository repository;
        private readonly ILogger<TimeController> logger;
        private readonly Tracer tracer;

        public TimeController(IDeviceRepository repository, IServiceProvider serviceProvider, ILogger<TimeController> logger)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.logger = logger;
            var tracerFactory = serviceProvider.GetService<TracerFactoryBase>();
            this.tracer = tracerFactory?.GetApplicationTracer();
        }

        // GET: api/time/dbtime
        [HttpGet("dbtime")]
        public async Task<DateTime> GetDbTimeAsync()
        {
            FailGenerator.FailIfNeeded(1);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                LogRequestHeaders();
            }

            var result = await repository.GetTimeFromSqlAsync();

            logger.LogInformation("{operation} result is {result}", nameof(repository.GetTimeFromSqlAsync), result);

            return result;
        }

        private void LogRequestHeaders()
        {
            var logText = new StringBuilder();
            logText.Append("Request headers: ");
            var first = true;
            foreach (var kv in Request.Headers)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    logText.Append(", ");
                }

                logText.Append(kv.Key).Append('=').Append(kv.Value);
            }

            logger.LogDebug(logText.ToString());
        }

        // GET: api/time/localday
        [HttpGet("localday")]
        public string GetLocalDay()
        {
            FailGenerator.FailIfNeeded(1);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                LogRequestHeaders();
            }

            var result = DateTime.Now.DayOfWeek.ToString();

            logger.LogInformation("Retrieved current day is {currentDay} at {time}", result, DateTime.UtcNow);
            return result;
        }
    }
}
