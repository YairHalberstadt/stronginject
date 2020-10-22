<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Factory Type Registration](#factory-type-registration)
  - [Implementing `IFactory<T>` and `IAsyncFactory<T>`](#implementing-ifactoryt-and-iasyncfactoryt)
  - [Disposal](#disposal)
  - [Example](#example)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Factory Type Registration

If a type implements `IFactory<T>` or `IAsyncFactory<T>` you can register it as a provider for `T` using the `[RegisterFactory] attribute. StrongInject will look for a suitable constructor to instantiate the type, as described [here](https://github.com/YairHalberstadt/stronginject/wiki/TypeRegistration#instantiation).

`RegisterFactoryAttribute` has 3 parameters.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RegisterFactoryAttribute : Attribute
{
    public RegisterFactoryAttribute(Type factoryType, Scope factoryScope = Scope.InstancePerResolution, Scope factoryTargetScope = Scope.InstancePerResolution);

    public Type FactoryType { get; }
    public Scope FactoryScope { get; }
    public Scope FactoryTargetScope { get; }
}
```

`factoryType` will be registered as a factory for any type `T` such that it implements `IFactory<T>` or `IAsyncFactory<T>`. A warning will be issued if it does not implement either interface.

`factoryScope` is used to control how often the factory is instantiated.

`factoryTargetScope` is used to control how often an instance of `T` will be created from the factory.

## Implementing `IFactory<T>` and `IAsyncFactory<T>`

See the documentation (WIP).

## Disposal

If `factoryType` implements `IDisposable` or `IAsyncDisposable`, an instance of `factoryType` will be automatically disposed by StrongInject once it is no longer used.

Any instances created by the factory will be passed to  `IFactory<T>.Release(T)`/`IAsyncFactory<T>.ReleaseAsync(T)` once they are no longer used. It is the factory's responsibility to dispose them. See the documentation (WIP).

## Example

```csharp
using StrongInject;
using System;

public class A : IDisposable { public void Dispose() { } }
public class Factory : IFactory<A>, IDisposable
{
    public A Create() => new A();

    public void Dispose() { }

    public void Release(A instance) => instance.Dispose();
}

[RegisterFactory(typeof(Factory), Scope.SingleInstance, Scope.InstancePerResolution)]
public partial class Container : IContainer<A> { }

var container = new Container();
container.Run(x => Console.WriteLine(x.ToString())); // Will create a new instance of Factory. Will call Factory.Create() to create an instance of A. After the lambda completes, will call factory.Release() to dispose of the instance of A.
container.Run(x => Console.WriteLine(x.ToString())); // Will reuse the existing instance of Factory since it is SingleInstance. Will call Factory.Create() to create an instance of A since it is InstancePerResolution. After the lambda completes, will call factory.Release() to dispose of the instance of A.
container.Dispose(); // Will dispose of the single instance of Factory
```
