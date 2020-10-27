# Factory Method Registration

A method may be marked as a Decorator Factory Method by applying the `[DecoratorFactory]` attribute:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class DecoratorFactoryAttribute : Attribute
{
    public DecoratorFactoryAttribute();
}
```

The `decoratedType` is defined as the return type of the method, unless the return type is `Task<T>`/`ValueTask<T>` in which case it is `T`, and the decorator factory method can only be used asynchronously.

The method must have exactly one parameter which is of the `decoratedType`. It can have other parameters of other types as well.

Whenever the `decoratedType` is resolved, an underlying instance of `decoratedType` will be resolved as normal. This instance will be used as a parameter to the method, and the result will be used as the resolved instance of `decoratedType` for the next decorator, or if it is the outermost decorator, for whatever originally required `decoratedType`.

## Generic Decorator Factory Methods

A Decorator Factory Method can be generic, and can have type parameter constraints. The same rules apply as for [Factory Methods](https://github.com/YairHalberstadt/stronginject/wiki/FactoryMethodRegistration#generic-factory-methods).

## Usage in Modules

The same rules apply as for [Factory Methods](https://github.com/YairHalberstadt/stronginject/wiki/FactoryMethodRegistration#usage-in-modules).

## Example

```csharp
using StrongInject;
using System;
using System.Linq;

public interface I { int Priority { get; } }
public class A : I { public int Priority => 1; }
public class B : I { public int Priority => 2; }

[Register(typeof(A), typeof(I))]
[Register(typeof(B), typeof(I))]
public partial class Container : IContainer<I[]>
{
    [DecoratorFactory]
    I[] OrderIArray(I[] iArray) => iArray.OrderByDescending(x => x.Priority).ToArray();
}

var container = new Container();
container.Run(x => {
    Console.WriteLine(x[0].GetType().ToString()); // prints "B"
    Console.WriteLine(x[1].GetType().ToString()); // prints "A"
}); // Resolves a new instance of I[], by resolving an instance of `A` and `B` and putting them in the array. Then calls OrderIArray with this instance, and uses the returned instance as the final resolution of `I[]`.
```
