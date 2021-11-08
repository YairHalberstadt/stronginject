using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StrongInject.Samples.AspNetCore.Controllers;
using StrongInject.Samples.AspNetCore.Services;
using System;

namespace StrongInject.Samples.AspNetCore
{
    [Register<WeatherForecastController>(Scope.InstancePerResolution)]
    [Register<WeatherForecastProvider, IWeatherForecastProvider>( Scope.InstancePerDependency)]
    [Register<WeatherSummarizer, IWeatherSummarizer>(Scope.SingleInstance)]
    [Register<UsersController>(Scope.InstancePerResolution)]
    [Register<DatabaseUsersCache, IUsersCache>(Scope.SingleInstance)]
    [Register<MockDatabase, IDatabase>(Scope.SingleInstance)]
    [RegisterDecorator<DatabaseDecorator, IDatabase>]
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
