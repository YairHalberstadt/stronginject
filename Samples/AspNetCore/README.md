# StrongInject.Samples.AspNetCore

## Overview

This sample demonstrates a simple rest API with two controllers. Both controllers are instantiated using StrongInject.

The weather forecast controller is reached at https://localhost:44383/weatherforecast?location=<SomeLocation> and returns a (randomly generated) weather forecast for that location.

The users controller is reached at https://localhost:44383/users and returns a list of users.

## Learning Points

This app demonstrates how to integrate StrongInject with ASP.NET Core using the StrongInject.Extensions.DependencyInjection.AspNetCore package.

It also demonstrates a number of other key features of StrongInject:

1. Usages of Scopes to control lifetimes. For example Controllers should be `InstancePerDependency` whilst  caches should be `SingleInstance`.
2. Passing `IServiceProvider` as a parameter to the container, and using it to resolve `ILogger<T>`, allowing for two way integration with other IOC containers.
3. Whilst `StrongInject` supports async resolution, Microsoft.Extensions.DependencyInjection does not. `DatabaseUsersCache` can only be prepared asynchronously so a different technique is used where requests on it become asynchronous instead.
4. Usage of a generic factory method to register `ILogger<T>` for all `T` at once.

## Notes

If you intend to use StrongInject to resolve all your controllers, then call `services.AddControllers().ResolveControllersThroughServiceProvider()` in `StartUp`. This tells ASP.NET Core to resolve all controllers through the service provider, but doesn't auto register them with the service provider. You can then register them yourselves manually.

If however you want to auto register all controllers with the service provider, and only override some of them with StrongInject, then you will need to call `services.AddControllers().AddControllersAsServices()` instead. This not only tells ASP.NET Core to resolve all controllers through the service provider, but also auto registers them as well. You will then need to make sure you remove these auto registrations when you manually overwrite the default registration by calling `services.ReplaceWithTransientServiceUsingContainer<Container, MyController>()` (or any of the related `ReplaceWithXServiceUsingContainer` methods).
