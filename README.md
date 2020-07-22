<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
![](https://github.com/yairhalberstadt/stronginject/workflows/.NET%20Core/badge.svg)
# stronginject
compile time dependency injection for .Net

## Table Of Contents

- [stronginject](#stronginject)
  - [Aims](#aims)
  - [Requirements](#requirements)
  - [Nuget](#nuget)
  - [Usage](#usage)
    - [Declaring a container](#declaring-a-container)
    - [Using a container.](#using-a-container)
    - [Registration](#registration)
      - [Scope](#scope)
      - [Modules](#modules)
      - [Factory Registrations](#factory-registrations)
      - [Providing registrations at runtime or integrating with other IOC containers](#providing-registrations-at-runtime-or-integrating-with-other-ioc-containers)
    - [Async initialization](#async-initialization)
    - [Disposal](#disposal)
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
[.Net 5 preview version](https://dotnet.microsoft.com/download/dotnet/5.0)

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

To use a container, you'll want to use the `RunAsync` extension methods defined in `StrongInject.ContainerExtensions`, so make sure you're `using StrongInject;`

```csharp
using StrongInject;

public class Program
{
  public static async Task Main()
  {
    System.Console.WriteLine(await new Container().RunAsync(x => x.ToString()));
  }
}
```

The `RunAsync` method ensures that all resolved objects are disposed after the call to `RunAsync`. Make sure not too leak those objects out of the delegate.
There are also overloads that allow you to pass in an async lambda, or a void returning lambda.

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

#### Factory Registrations

Sometimes a type requires more complex construction than just calling the constructor. For example you might want to hard code some parameters, or call a factory method. Some types don't have the correct constructors to be registered directly.

For such cases the `IFactory<T>` interface exists.

When you register a type as `IFactory<T>` it will automatically register it as `T` as well. An instance of `T` will be constructed by calling `IFactory<T>.CreateAsync()`.

```csharp
using StrongInject;
using System.Threading.Tasks;

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}
public class InterfaceArrayFactory : IFactory<IInterface[]>
{
  private A _a;
  private B _b;
  public InterfaceArrayFactory(A a, B b) => (_a, _b) = (a, b);
  public ValueTask<IInterface[]> CreateAsync() => new ValueTask<IInterface[]>(new IInterface[] {_a, _b});
}

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(InterfaceArrayFactory), typeof(IFactory<IInterface[]>))]
public partial class Container : IContainer<IInterface[]> {}
```

The scope of the factory and the factory target is controlled seperately. This allows you to e.g. have a singleton factory, but call `CreateAsync` on every resolution:

```csharp
[Registration(typeof(InterfaceArrayFactory), scope: Scope.SingleInstance, factoryTargetScope: Scope.InstancePerResolution, typeof(IFactory<IInterface[]>))]
```

#### Providing registrations at runtime or integrating with other IOC containers

What if you need to provide configuration for a registration at runtime? Or alternatively what if you need to integrate with an existing container?

For that you can use the `IInstanceProvider<T>` interface. Any fields of a container which are or implement `IInstanceProvider<T>` will provide/override any existing registrations for `T`.

Here is a full fledged example of how you could provide configuration for a registration at runtime, whilst still getting the full benefit of the IOC container to create your types. Of course many cases will be simpler, and not require usage of both a factory and an instanceProvider.

```csharp
using StrongInject;
using System.Threading.Tasks;

public interface IInterface { }
public class A : IInterface { }
public class B : IInterface { }

public enum InterfaceToUse
{
    UseA,
    UseB
}

public class InstanceProvider : IInstanceProvider<InterfaceToUse>
{
    InterfaceToUse _interfaceToUse;
    public InstanceProvider(InterfaceToUse interfaceToUse) => _interfaceToUse = interfaceToUse;
    public async ValueTask<InterfaceToUse> GetAsync() => _interfaceToUse;
}

public class InterfaceFactory : IFactory<IInterface>
{
    private A _a;
    private B _b;
    private InterfaceToUse _interfaceToUse;
    public InterfaceFactory(A a, B b, InterfaceToUse interfaceToUse) => (_a, _b, _interfaceToUse) = (a, b, interfaceToUse);
    public ValueTask<IInterface> CreateAsync() => new ValueTask<IInterface>(_interfaceToUse == InterfaceToUse.UseA ? (IInterface)_a : _b);
}

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(InterfaceFactory), typeof(IFactory<IInterface>))]
public partial class Container : IContainer<IInterface>
{
    private InstanceProvider _instanceProvider;
    public Container(InstanceProvider instanceProvider) => _instanceProvider = instanceProvider;
}
```

`GetAsync` is called once per resolution (equiavalent to Instance Per Resolution scope). Of course the implementation is free to returna singleton or not.

### Async initialization

If your type implements `IRequiresInitialization`, `InitializeAsync` will be called after construction.

Here is a full fledged example where data will be loaded from the database as part of resolution:

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

public class PasswordChecker : IRequiresInitialization, IAsyncDisposable
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

public class DbInstanceProvider : IInstanceProvider<IDb>
{
    private readonly IDb _db;

    public DbInstanceProvider(IDb db)
    {
        _db = db;
    }

    public ValueTask<IDb> GetAsync()
    {
        return new ValueTask<IDb>(_db);
    }

    public ValueTask ReleaseAsync(IDb instance)
    {
        return default;
    }
}

[Registration(typeof(PasswordChecker), Scope.SingleInstance)]
public partial class Container : IContainer<PasswordChecker>
{
    public DbInstanceProvider _dbInstanceProvider;

    public Container(DbInstanceProvider dbInstanceProvider)
    {
        _dbInstanceProvider = dbInstanceProvider;
    }
}
```

### Disposal

Once a call to `RunAsync` is complete, any Instance Per Resolution or Instance Per Dependency instances created as part of the call to `RunAsync` will be disposed.

If the types implement `IAsyncDisposable` it will be prefferred over `IDisposable`. `Dispose` and `DisposeAsync` will not both be called, just `DisposeAsync`.

Since an `InstanceProvider<T>` is free to create a new instance every time or return a singleton, StrongInject cannot call dispose directly. Instead it calls `InstanceProvider<T>.ReleaseAsync(T instance)`. The instanceProvider is then free to dispose the class or not. When referencing the .Net Standard 2.1 package `ReleaseAsync` has a default implementation which does nothing. You only need to implement it if you want custom behaviour. If you reference the .Net Standard 2.0 package you will need to implement it either way.

## Contributing

This is currently in preview, meaning we can (and will) make API breaking changes. Now is the best time to file suggestions if you feel like the API could be approved.

Similiarly please do open issues if you spot any bugs.

Please feel free to work on any open issue and open a PR. Ideally open an issue before working on something, so that the effort doesn't go to waste if it's not suitable.
