using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StrongInject;
using StrongInject.Samples.AspNetCore.Controllers;
using StrongInject.Samples.AspNetCore.Services;
using System;

namespace StrongInject.Samples.AspNetCore
{
    [Register(typeof(WeatherForecastController), Scope.InstancePerResolution)]
    [Register(typeof(WeatherForecastProvider), Scope.InstancePerDependency, typeof(IWeatherForecastProvider))]
    [Register(typeof(WeatherSummarizer), Scope.SingleInstance, typeof(IWeatherSummarizer))]
    public partial class Container : IContainer<WeatherForecastController>
    {
        private readonly IServiceProvider _serviceProvider;

        public Container(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Factory] private ILogger<T> GetLogger<T>() => _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}
