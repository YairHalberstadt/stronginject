using StrongInject.Samples.AspNetCore;
using System;

namespace StrongInject.Samples.AspNetCore.Services
{
    public class WeatherForecastProvider : IWeatherForecastProvider
    {
        public WeatherForecastProvider(string location, IWeatherSummarizer weatherSummarizer)
        {
            _location = location;
            _weatherSummarizer = weatherSummarizer;
        }

        private readonly Random _random = new Random();
        private readonly string _location;
        private readonly IWeatherSummarizer _weatherSummarizer;

        public WeatherForecast GetForecast(DateTime day)
        {
            var temperatureC = _random.Next(-20, 55);
            return new WeatherForecast
            {
                Location = _location,
                Date = day,
                TemperatureC = temperatureC,
                Summary = _weatherSummarizer.Summarize(temperatureC),
            };
        }
    }
}
