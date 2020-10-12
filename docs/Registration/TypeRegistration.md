# Type Registration

You can register a type either as itself, or as its base classes/interfaces. StrongInject will look for a suitable constructor to use to instantiate it.

To register a type, add the `[Register]` attribute to a container or module.

The `RegisterAttribute` has three parameters:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RegisterAttribute : Attribute
{
    public RegisterAttribute(Type type, params Type[] registerAs);

    public RegisterAttribute(Type type, Scope scope, params Type[] registerAs);

    public Type Type { get; }
    public Type[] RegisterAs { get; }
    public Scope Scope { get; }
}
```

The `type` parameter is the type that StrongInject will try to instantiate by looking for a suitable constructor.

The `scope` parameter specifies the [Scope](https://github.com/YairHalberstadt/stronginject/wiki/Registration#scopes) of the registration. This is optional, and is `Scope.InstancePerResolution` by default.

The `registerAs` is a params parameter, and specifies which types the registered type should be considered a registration for. Stronginject will only use this registration to resolve types specified in the `registerAs` list. If the list is empty it will be considered a registration for itself by default. `type` must be a subtype of all types specified in `registerAs`.

## Instantiation

It is an error if `type` does not have a public constructor.

It is an error if `type` has more than one public constructor with parameters.

If `type` has 1 public constructor, that constructor will be used to instantiate an instance of `type`.

If `type` has 1 parameterless public constructor, and 1 public constructor with parameters, the constructor with parameters will be used to instantiate an instance of `type`.

## Disposal

If `type` implements `IDisposable` or `IAsyncDisposable`, an instance of `type` will be automatically disposed by StrongInject once it is no longer used.

## Example

```csharp
using StrongInject;
using System;

public class A : IDisposable { public void Dispose(){} }
public class B : IDisposable { public void Dispose(){} }
public interface I {}
public class C : I { public C(A a, B b)}

[Register(typeof(A), Scope.SingleInstance)] // No types specified for `registerAs`, will be registered as type `A`.
[Register(typeof(B))] // No `scope` specified, Scope will be InstancePerResolution. No types specified for `registerAs`, will be registered as type `B`.
[Register(typeof(C), typeof(C), typeof(I))] // No `scope` specified, Scope will be InstancePerResolution. Registered as type `C` and as type `I`.
public class Container : IContainer<I> {}

var container = new Container();
container.Run(x => Console.WriteLine(x.ToString())); // Will create a new instance of `C` which is registered as `I`, and new instances of `A` and `B` to satisfy `C`'s constructor. `B` will be disposed once the lambda completes.
container.Run(x => Console.WriteLine(x.ToString())); // Will create a new instance of `C` which is registered as `I`, and new instances of `B` to satisfy `C`'s constructor. The instance of `A` will be reused from the previous invocation, since it is SingleInstance. `B` will be disposed once the lambda completes.
container.Dispose(); // Will dispose the single instance of `A`.
```
