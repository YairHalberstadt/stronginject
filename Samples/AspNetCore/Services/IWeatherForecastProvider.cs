using StrongInject.Samples.AspNetCore;
using System;

namespace StrongInject.Samples.AspNetCore.Services
{
    public interface IWeatherForecastProvider
    {
        WeatherForecast GetForecast(DateTime day);
    }
}
