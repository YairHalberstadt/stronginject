<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Decorator Type Registration](#decorator-type-registration)
  - [Constructor](#constructor)
  - [Disposal](#disposal)
  - [Scope](#scope)
  - [Example](#example)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Decorator Type Registration

You can register a type as a decorator for it's base classes/interfaces. StrongInject will look for a suitable constructor to use to instantiate it.

To register a type as a decorator, add the `[RegisterDecorator]` attribute to a container or module.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RegisterDecoratorAttribute : Attribute
{
    public RegisterDecoratorAttribute(Type type, Type decoratedType, DecoratorOptions decoratorOptions = DecoratorOptions.Default);

    public Type Type { get; }
    public Type DecoratedType { get; }
    public DecoratorOptions DecoratorOptions { get; }
}
```

`type` must be a subtype of `decoratedType`.

Whenever the decoratedType is resolved, an underlying instance of `decoratedType` will be resolved as normal. This instance will be used as a parameter to `type`, and `type` will be used as the resolved instance of `decoratedType` for the next decorator, or if it is the outermost decorator, for whatever originally required `decoratedType`.

`decoratorOptions` is used to control [whether the decorator is disposed or not](https://github.com/YairHalberstadt/stronginject/wiki/Decorators#disposal).

## Constructor

It is an error if type does not have a public constructor with parameters.

It is an error if type has more than one public constructor with parameters.

It is an error if the public constructor with parameters does not have exactly one parameter which is of the `decoratedType`. It can have other parameters of other types as well.

## Disposal

If type implements IDisposable or IAsyncDisposable, an instance of type will be automatically disposed by StrongInject once it is no longer used.

It is recommended to not dispose of the underlying instance of `decoratedType`, as StrongInject will handle disposal of this itself.

## Scope

`RegisterDecorator` does not have a `scope` parameter because decorators are created whenever an instance of the underlying type is created. Hence they're scope is entirely determined by that of the underlying type.

## Example

```csharp
using StrongInject;
using System;

public interface IService { string GetMessage(); }
public class Service : IService { public string GetMessage() => "Hello World"; }
public class ServiceDecorator : IService
{
    private readonly IService _impl;
    private readonly Logger _logger;
    public ServiceDecorator(IService impl, Logger logger) => (_impl, _logger) = (impl, logger);
    public string GetMessage()
    {
        var message = _impl.GetMessage();
        _logger.Log("Message was " + message);
        return message;
    }
}
public class Logger { public void Log(string str) => Console.WriteLine(str); }

[Register(typeof(Service), typeof(IService))]
[Register(typeof(Logger))]
[RegisterDecorator(typeof(ServiceDecorator), typeof(IService))]
public partial class Container : IContainer<IService> {}

var container = new Container();
container.Run(x => Console.WriteLine(x.GetMessage())); // Will create a new instance of Service and Logger, and pass them as parameters to the ServiceDecorator constructor. The instance of ServiceDecorator will be used as the parameter to the lambda.
```

The above example will print:

```
Message was Hello World
Hello World
```
