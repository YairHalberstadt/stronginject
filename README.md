<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
![](https://github.com/yairhalberstadt/stronginject/workflows/.NET%20Core/badge.svg)
# stronginject
compile time dependency injection for .Net

## Table Of Contents

- [Aims](#aims)
- [Requirements](#requirements)
- [Nuget](#nuget)
- [Usage](#usage)
  - [Declaring a container](#declaring-a-container)
  - [Using a container.](#using-a-container)
  - [Registration](#registration)
    - [Scope](#scope)
    - [Modules](#modules)
    - [Factories](#factories)
    - [Providing registrations at runtime or integrating with other IOC containers](#providing-registrations-at-runtime-or-integrating-with-other-ioc-containers)
  - [Delegate Support](#delegate-support)
  - [Post Constructor Initialization](#post-constructor-initialization)
  - [Async Support](#async-support)
  - [Disposal](#disposal)
  - [Thread Safety](#thread-safety)
- [Contributing](#contributing)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Aims

1. **Compile time checked dependency injection.** If the type you're resolving isn't registered you get an error at compile time, not runtime.
2. **Fast.** There's no dictionary lookups, no runtime code generation. Just the fastest code it's possible to generate to resolve your type.
3. **Encourage best practices.** You can't use the container as a service locator. You can't forget to dispose the resolved types.
4. **No reflection or runtime code generation.** Instead StrongInject uses roslyn Source Generators, meaning it's fast, and works well on UWP/IOS too.
5. **Async support.** StrongInject fully supports async initialization and disposal, a feature sorely lacking in many IOC containers.

## Requirements

[Visual Studio preview version](https://visualstudio.microsoft.com/vs/preview/)
[.NET 5 preview version](https://dotnet.microsoft.com/download/dotnet/5.0)

## Nuget

https://www.nuget.org/packages/StrongInject/

We recommend you use floating versions for now, as `StrongInject` is still in preview and changing rapidly. 
`<PackageReference Include="StrongInject" Version="0.0.1-CI-*" />`

## Usage

### Declaring a container
To create a container for a specific type, declare your container partial and inherit from `StrongInject.IContainer<T>`:

```csharp
using StrongInject;

public class A {}

[Registration(typeof(A))]
public partial class Container : IContainer<A> {}
```

If it's possible to resolve the type parameter StrongInject will generate the implementation of IContainer for you. Else it will produce an error diagnostic.

You can implement `IContainer<T>` for different values of `T`. They will all share SingleInstance dependencies.

```csharp
using StrongInject;

public class A {}
public class B {}

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>, IContainer<B> {}
```

### Using a container.

To use a container, you'll want to use the `Run` extension methods defined in `StrongInject.ContainerExtensions`, so make sure you're `using StrongInject;`

The `Run` method on `IContainer<T>` takes a `Func<T>`. It resolves an instance of `T`, calls the func, disposes of any dependencies which require disposal, and then returns the result of the func. This ensures that you can't forget to dispose any dependencies, but you must make sure not too leak those objects out of the delegate. There are also overloads that allow you to pass in a void returning lambda.

```csharp
using StrongInject;

public class Program
{
  public static void Main()
  {
    System.Console.WriteLine(new Container().Run(x => x.ToString()));
  }
}
```

### Registration

As you saw above, you can register a type with a container using the `RegistrationAttribute`:

```csharp
using StrongInject;

public class A {}
public class B {}

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>, IContainer<B> {}
```

All the dependencies of the container type parameter must be registered or you will get a compile time error.

By default `[Registration(typeof(A))]` will register an type `A` as itself. You can however register a type as any base type or implemented interface:

```csharp
using StrongInject;

public class BaseBase {}
public interface IBase {}
public class Base : BaseBase, IBase {}
public interface IA {}
public class A : Base, IA {}

[Registration(typeof(A), typeof(IA), typeof(IBase), typeof(BaseBase))]
public partial class Container : IContainer<BaseBase> {}
```

If you do so, you will have to explicitly also register it as itself if that is desired: `[Registration(typeof(A), typeof(A), typeof(IA), typeof(IBase), typeof(BaseBase))]`

If there is a single public non-parameterless constructor, StrongInject will use that to construct the type. If there is no public non-parameterless constructor StrongInject will use the parameterless constructor if it exists and is public. Else it will report an error.

#### Scope

The scope of a registration determines how often a new instance is created, how long it lives, and who uses it.

It can be set as the second parameter of a registration:

```csharp
using StrongInject;

public class A {}
public interface IB {}
public class B : IB {}

[Registration(typeof(A), Scope.SingleInstance)]
[Registration(typeof(B), Scope.InstancePerResolution, typeof(IB))]
public partial class Container : IContainer<A>, IContainer<IB> {}
```

There are currently 3 diferent scopes:

**Instance Per Resolution**

This is the default scope.

A single instance is shared between all dependencies created for a single resolution.
For example if 'A' debends on 'B' and 'C', and 'B' and 'C' both depend on an instance of 'D',
then when 'A' is resolved 'B' and 'C' will share the same instance of 'D'.

Note every SingleInstance dependency defines a seperate resolution, 
so if 'B' and/or 'C' are SingleInstance they would not share an instance of 'D'.

**Instance Per Dependency**

A new instance is created for every usage.
For example even if type 'B' appears twice in the constructor of 'A',
two different instances will be passed into the constructor.

**SingleInstance**

A single instance will be shared across all dependencies, from any resolution

#### Modules

You can add registrations to any type, and then import them using the `ModuleRegistrationAttribute`. This allows you to create reusable modules of common registrations.

```csharp
using StrongInject;

public class A {}

[Registration(typeof(A))]
public class Module {}

[ModuleRegistration(typeof(Module))]
public partial class Container : IContainer<A> {}
```

If you import multiple modules, and they both register the same type differently, you will get an error.

There are two ways to solve this:

1. Register the type directly. This will override the registrations in imported modules.
2. Exclude the registration from one of the modules when you import it: `[ModuleRegistration(typeof(Module), exclusionList: new [] { typeof(A) })]`

#### Factories

Sometimes a type requires more complex construction than just calling the constructor. For example you might want to hard code some parameters, or call a factory method. Some types don't have the correct constructors to be registered directly.

In such cases you can write a method returning the type you want to construct and mark it with the `[Factory]` attribute.

```csharp
using StrongInject;

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<IInterface[]>
{
    [Factory] private IInterface[] CreateInterfaceArray(A a, B b) => new IInterface[] { a, b };
}
```

You can set the scope of the factory method (how often it's called), by passing in the scope parameter: `[Factory(Scope.SingleInstance)]`.

If the factory method is defined on the container type, it can be private or public, instance or static. However if you want to export the factory method as part of a module it must be public and static.

```csharp
using StrongInject;

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}

public class Module
{
    [Factory] public static IInterface[] CreateInterfaceArray(A a, B b) => new IInterface[] { a, b };
}

[Registration(typeof(A))]
[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IContainer<IInterface[]>
{
}
```

If the factory method returns `Task<T>` or `ValueTask<T>` it will be considered an async registration for `T` (see [below](#async-support)).

In some cases you want to maintain some state in your factory, or control disposal. For such cases the `IFactory<T>` interface exists.

You can register a type implementing `IFactory<T>` as a Factory Registration.
This will automatically register it as both `IFactory<T>` and `T`. An instance of `T` will be constructed by calling `IFactory<T>.Create()`.

```csharp
using StrongInject;
using System.Buffers;

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}
public record InterfaceArrayFactory(A A, B B) : IFactory<IInterface[]>
{
    public IInterface[] Create()
    {
        var array = ArrayPool<IInterface>.Shared.Rent(2);
        array[0] = A;
        array[1] = B;
        return array;
    }
    public void Release(IInterface[] instance)
    {
        ArrayPool<IInterface>.Shared.Return(instance);
    }
}

[Registration(typeof(A))]
[Registration(typeof(B))]
[FactoryRegistration(typeof(InterfaceArrayFactory))]
public partial class Container : IContainer<IInterface[]> { }
```

Whilst a factory doesn't have to be a record, doing so significantly shortens the amount of code you have to write.

The scope of the factory and the factory target is controlled separately. This allows you to e.g. have a singleton factory, but call `CreateAsync` on every resolution:

```csharp
[FactoryRegistration(typeof(InterfaceArrayFactory), scope: Scope.SingleInstance, factoryTargetScope: Scope.InstancePerResolution, typeof(IFactory<IInterface[]>))]
```

If a factory implements `IFactory<T>` for multiple `T`s it will be registered as a factory for all of them. 

#### Providing registrations at runtime or integrating with other IOC containers

What if you need to provide configuration for a registration at runtime? Or alternatively what if you need to integrate with an existing container?

For that you can use the `IInstanceProvider<T>` interface. Any fields of a container which are or implement `IInstanceProvider<T>` will provide/override any existing registrations for `T`.

Here is a full fledged example of how you could provide configuration for a registration at runtime, whilst still getting the full benefit of the IOC container to create your types. Of course many cases will be simpler, and not require usage of both a factory and an instanceProvider.

```csharp
using StrongInject;

public interface IInterface { }
public class A : IInterface { }
public class B : IInterface { }

public enum InterfaceToUse
{
    UseA,
    UseB
}

public record InstanceProvider(InterfaceToUse InterfaceToUse) : IInstanceProvider<InterfaceToUse>
{
    public InterfaceToUse Get() => InterfaceToUse;
}

public record InterfaceFactory(A A, B B, InterfaceToUse InterfaceToUse) : IFactory<IInterface>
{
    public IInterface Create() => InterfaceToUse == InterfaceToUse.UseA ? (IInterface)A : B;
}

[Registration(typeof(A))]
[Registration(typeof(B))]
[FactoryRegistration(typeof(InterfaceFactory))]
public partial class Container : IContainer<IInterface>
{
    private readonly InstanceProvider _instanceProvider;
    public Container(InstanceProvider instanceProvider) => _instanceProvider = instanceProvider;
}
```

`Get` is called once per resolution (equiavalent to Instance Per Resolution scope). Of course the implementation is free to return a singleton or not.

### Delegate Support

StrongInject can automatically resolve non-void reurning delegates even if they're not registered. It tries to resolve the return type. The delegate parameters can be used in the resolution, and will override any existing resolutions.

There are two reasons you might want to use delegate resolution:

1. To return a new instance of the return type on every call:

```csharp
using System;
using StrongInject;

public class A
{
  public A(Func<B> fB) => Console.WriteLine(fB() != fB()); //prints true
}

public class B{}

[Registration(typeof(A))]
[Registration(typeof(B))]
public class Container : IContainer<A> {}
```

2. To provide parameters which are not available at resolution time:

```csharp
using System;
using StrongInject;

public class Server
{
  private Handler _frobbingHandler;
  private Handler _nonFrobbingHandler;
  public Server(Func<bool, Handler> handlerFunc) => (_frobbingHandler, _nonFrobbingHandler) = (handlerFunc(true), handlerFunc(false));
  
  public bool HandleRequest(Request request, bool shouldFrob) => shouldFrob ? _frobbingHandler.HandleRequest(request) : _nonFrobbingHandler.HandleRequest(request);
}

public class Handler
{
  public Handler(bool shouldFrob) => ...
}

[Registration(typeof(Server))]
[Registration(typeof(Handler))]
public class Container : IContainer<Server> {}
```

If the return type can only be resolved asynchronously (see [below](#async-support)), the delegate must return `Task<T>` or `ValueTask<T>`. e.g.

```csharp
using System;
using StrongInject;
using System.Threading.Tasks;

public class Server
{
  private Func<bool, Task<Handler>> _handlerFunc;
  public Server(Func<bool, Task<Handler>> handlerFunc) => _handlerFunc = handlerFunc;
  
  public async Task<bool> HandleRequest(Request request, bool shouldFrob) => (await _handlerFunc(shouldFrob)).HandleRequest(request);
}

public class Handler : IRequiresAsyncInitialization
{
  public Handler(bool shouldFrob) => ...
  public async ValueTask ResolveAsync() => ...
}

[Registration(typeof(Server))]
[Registration(typeof(Handler))]
public class Container : IContainer<Server> {}
```

### Post Constructor Initialization

If your type implements `IRequiresInitialization`, `Initialize` will be called after construction.
Whilst this is only useful in a few edge cases for synchronous methods, `IRequiresAsyncInitialization` is extremely useful as constructors cannot be async. Therefore I'll leave an example of using this API for the section on async support. 

### Async Support

Every interface use by StrongInject has an asynchronous counterpart.
Theres `IAsyncContainer`, `IAsyncFactory`, `IRequiresAsyncInitialization`, and `IAsyncInstanceProvider`.

You can resolve an instance of `T` asynchronously from an `IAsyncContainer<T>` by calling `StrongInject.AsyncContainerExtensions.RunAsync`. RunAsync has overloads allowing you to pass in  sync or async lambda. As such `IAsycContainer<T>` is useful even if resolution is completely synchronous if usage is asynchronous.

It is an error resolving `T` in an `IContainer<T>` depends on an asynchronous dependency.

A type can implement both `IContainer<T1>` and `IAsyncContainer<T2>`. They will share single instance depdendencies.

Here is a full fledged example where data will be loaded from the database as part of resolution using `IRequiresAsyncInitialization`:

```csharp
using StrongInject;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDb
{
    Task<Dictionary<string, string>> GetUserPasswordsAsync();
}

public class PasswordChecker : IRequiresAsyncInitialization, IAsyncDisposable
{
    private readonly IDb _db;

    private Dictionary<string, string> _userPasswords;

    private Timer _timer;

    public PasswordChecker(IDb db)
    {
        _db = db;
    }

    public async ValueTask InitializeAsync()
    {
        _userPasswords = await _db.GetUserPasswordsAsync();
        _timer = new Timer(async _ => { _userPasswords = await _db.GetUserPasswordsAsync(); }, null, 60000, 60000);
    }

    public bool CheckPassword(string user, string password) => _userPasswords.TryGetValue(user, out var correctPassword) && password == correctPassword;

    public ValueTask DisposeAsync()
    {
        return _timer.DisposeAsync();
    }
}

public record DbInstanceProvider(IDb Db) : IInstanceProvider<IDb>
{
    public IDb Get()
    {
        return Db;
    }

    public void Release(IDb instance) {}
}

[Registration(typeof(PasswordChecker), Scope.SingleInstance)]
public partial class Container : IAsyncContainer<PasswordChecker>
{
    private readonlyDbInstanceProvider _dbInstanceProvider;

    public Container(DbInstanceProvider dbInstanceProvider)
    {
        _dbInstanceProvider = dbInstanceProvider;
    }
}

public static class Program
{
  public static async Task Main(string[] args)
  {
    await new Container(new DbInstanceProvider(new Db())).RunAsync(x => 
      Console.WriteLine(x.CheckPassword(args[0], args[1])
        ? "Password is valid"
        : "Password is invalid"));
  }
}
```

### Disposal

Once a call to `Run` or `RunAsync` is complete, any Instance Per Resolution or Instance Per Dependency instances created as part of the call to `Run` or `RunAsync` will be disposed.

In `RunAsync` if the types implement `IAsyncDisposable` it will be preferred over `IDisposable`. `Dispose` and `DisposeAsync` will not both be called, just `DisposeAsync`.
In `Run`, `IAsyncDisposable` will be ignored. Only `Dispose` will ever be called.

Since an `InstanceProvider<T>` is free to create a new instance every time or return a singleton, StrongInject cannot call dispose directly. Instead it calls `InstanceProvider<T>.Release(T instance)`. The instanceProvider is then free to dispose the class or not. When referencing the .NET Standard 2.1 package `Release` has a default implementation which does nothing. You only need to implement it if you want custom behaviour. If you reference the .NET Standard 2.0 package you will need to implement it either way.

Single Instance dependencies and their dependencies are disposed when the container is disposed. If the container implements `IAsyncDisposable` it must be disposed asynchronously even if it also implements `IDisposable`.

Note that dependencies may not be disposed in the following circumstances:
1. Resolution throws
2. Disposal of other dependencies throws.

### Thread Safety

StrongInject provides the following thread safety guarantees:
1. Resolution is thread safe, so long as it doesn't call back into the container recursively (e.g. a factory calling `container.RunAsync`).
2. If the container is disposed during resolution, then either dependencies will be created by the resolution, and will then be disposed, or resolution will throw and no dependencies will be created. Dependencies will not be created and then not disposed.
3. A SingleInstance dependency will never be created more than once.

## Contributing

This is currently in preview, meaning we can (and will) make API breaking changes. Now is the best time to file suggestions if you feel like the API could be approved.

Similiarly please do open issues if you spot any bugs.

Please feel free to work on any open issue and open a PR. Ideally open an issue before working on something, so that the effort doesn't go to waste if it's not suitable.
