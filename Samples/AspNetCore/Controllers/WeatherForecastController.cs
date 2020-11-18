using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StrongInject.Samples.AspNetCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongInject.Samples.AspNetCore.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly Func<string, IWeatherForecastProvider> _weatherForecastProvider;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(Func<string, IWeatherForecastProvider> weatherForecastProvider, ILogger<WeatherForecastController> logger)
        {
            _weatherForecastProvider = weatherForecastProvider;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get(string location)
        {
            _logger.LogInformation($"Requesting weather forecasts at {location}");
            var rng = new Random();
            return Enumerable.Range(1, 5)
                .Select(index => _weatherForecastProvider(location).GetForecast(DateTime.Now.AddDays(index)))
                .ToArray();
        }
    }
}
