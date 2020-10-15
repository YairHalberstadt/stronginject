# Factory Method Registration

A method may be marked as a Factory Method by applying the `[Factory]` attribute:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class FactoryAttribute : Attribute
{
    public FactoryAttribute(Scope scope = Scope.InstancePerResolution);
    public Scope Scope { get; }
}
```

A factory method is registered as a provider for the return type of the factory method. If the return type is `Task<T>`/`ValueTask<T>` then it is a provider for `T`, and can only be resolved asynchronously.

A new instance will be created by resolving all the parameters of the method and calling the method. The `scope` parameter controls how often the factory method will be called.

## Generic Factory Methods

A Factory Method can be generic, and can have type parameter constraints.

It is an error if not all the type parameters are used in the return type.

When resolving an instance of type `T`, StrongInject tries to see if we can construct `T` out of the return type by substituting type parameters for concrete types. If so we check to make sure these substitutions match the type parameter constraints. If the constraints do match, the constructed method with the type parameters substituted is used as a registration for `T`.

## Usage in Modules

A Factory Method in a module must be `public` and `static` in order to be exported.

## Example

```csharp
using StrongInject;
using System;
using System.Collections.Generic;

public class A { }

public class Container : IContainer<List<A>>
{
    [Factory(Scope.SingleInstance)] private A CreateA() => new A(); // Registration for A
    [Factory] private List<T> CreateList<T>(T t) => new List<T>{ t }; // Registration for List<A> by substituting T for A
    [Factory] private List<T> CreateList<T>() where T : struct => new List<T>{ new T() }; // Not a registration for List<A> because substituting T for A does not match the struct constraint
}

var container = new Container();
container.Run(x => Console.WriteLine(x.ToString())); // Resolves a new instance `A` by calling CreateA(), and calls CreateList<A>(A) with it as a parameter.
container.Run(x => Console.WriteLine(x.ToString())); // Reuses the single instance of `A`, and calls CreateList<A>(A) with it as a parameter.
```
