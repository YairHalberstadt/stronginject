using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StrongInject.Samples.AspNetCore.Controllers;
using StrongInject.Samples.AspNetCore.Services;
using System;

namespace StrongInject.Samples.AspNetCore
{
    [Register(typeof(WeatherForecastController), Scope.InstancePerResolution)]
    [Register(typeof(WeatherForecastProvider), Scope.InstancePerDependency, typeof(IWeatherForecastProvider))]
    [Register(typeof(WeatherSummarizer), Scope.SingleInstance, typeof(IWeatherSummarizer))]
    [Register(typeof(UsersController), Scope.InstancePerResolution)]
    [Register(typeof(DatabaseUsersCache), Scope.SingleInstance, typeof(IUsersCache))]
    [Register(typeof(MockDatabase), Scope.SingleInstance, typeof(IDatabase))]
    [RegisterDecorator(typeof(DatabaseDecorator), typeof(IDatabase))]
    public partial class Container : IContainer<WeatherForecastController>, IContainer<UsersController>
    {
        private readonly IServiceProvider _serviceProvider;

        public Container(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Factory] private ILogger<T> GetLogger<T>() => _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}
