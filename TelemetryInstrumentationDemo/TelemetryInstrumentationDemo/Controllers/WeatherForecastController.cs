using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace TelemetryInstrumentationDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            // Documentation: https://opentelemetry.io/docs/instrumentation/net/getting-started/


            // Get the TraceId for the Request
            var traceId = Activity.Current.Context.TraceId.ToString();

            // Add any custom tags to the trace
            Activity.Current.AddTag("request", "initial");

            // Add additional baggage to the trace
            Activity.Current.AddBaggage("foo", "bar");

            // Manually implement a new trace, and add additional tags to that trace
            var activityEvent = DiagnosticsConfig.ActivitySource.StartActivity("New Request");
            activityEvent?.SetTag("foo", 1);
            activityEvent?.SetTag("bar", "Request");

            // Your logging solution will vary, but to be able to link logs to traces, you'll want to include in your log line a key/value pair of traceId = traceId
            // Further configuration for connecting the two in grafana, will be set up in the Data Source configs, so that loki can identify a traceId based off a regex pattern. 
            _logger.Log(LogLevel.Information, $"traceId={traceId}");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                TraceId = traceId
            })
            .ToArray();
        }
    }
}