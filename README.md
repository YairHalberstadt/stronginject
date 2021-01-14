![](https://github.com/yairhalberstadt/stronginject/workflows/.NET%20Core/badge.svg)
[![Join the chat at https://gitter.im/stronginject/community](https://badges.gitter.im/stronginject/community.svg)](https://gitter.im/stronginject/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/StrongInject)](https://www.nuget.org/packages/StrongInject)

![StrongInject](resources/logo-horizontal.png)

**Compile Time Dependency Injection For C#**

Logo kindly contributed by [@onelioubov](https://github.com/onelioubov) and [@khalidabuhakmeh](https://github.com/khalidabuhakmeh)

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Aims](#aims)
- [Requirements](#requirements)
- [Nuget](#nuget)
- [How It Works](#how-it-works)
- [Usage](#usage)
  - [Sample Projects](#sample-projects)
      - [Asp.Net Core/Microsoft.Extensions.DependencyInjection](#aspnet-coremicrosoftextensionsdependencyinjection)
      - [Console Application](#console-application)
      - [Xamarin Application](#xamarin-application)
    - [Real World Projects Using Stronginject](#real-world-projects-using-stronginject)
  - [Declaring a container](#declaring-a-container)
  - [Using a container.](#using-a-container)
  - [Registration](#registration)
    - [Basics](#basics)
    - [Scope](#scope)
    - [Modules](#modules)
    - [Instance fields and properties](#instance-fields-and-properties)
    - [Factories](#factories)
    - [Generic Factory Methods](#generic-factory-methods)
    - [Decorators](#decorators)
    - [Providing registrations at runtime or integrating with other IOC containers](#providing-registrations-at-runtime-or-integrating-with-other-ioc-containers)
    - [How StrongInject picks which registration to use](#how-stronginject-picks-which-registration-to-use)
  - [Delegate Support](#delegate-support)
  - [Post Constructor Initialization](#post-constructor-initialization)
  - [Async Support](#async-support)
    - [Parallel Resolution](#parallel-resolution)
  - [Resolving all instances of a type](#resolving-all-instances-of-a-type)
  - [Optional Parameters](#optional-parameters)
  - [Disposal](#disposal)
  - [Thread Safety](#thread-safety)
  - [Inbuilt Modules](#inbuilt-modules)
- [Product Roadmap](#product-roadmap)
- [Contributing](#contributing)
- [Need Help?](#need-help)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Aims

1. **Compile time checked dependency injection.** If the type you're resolving isn't registered you get an error at compile time, not runtime.
2. **Fast.** There's no dictionary lookups, no runtime code generation. Just the fastest code it's possible to generate to resolve your type.
3. **Encourage best practices.** You can't use the container as a service locator. You can't forget to dispose the resolved types.
4. **No reflection or runtime code generation.** Instead StrongInject uses roslyn Source Generators, meaning it's fast, and works well on UWP/IOS too. This also means it's linker friendly - see https://devblogs.microsoft.com/dotnet/app-trimming-in-net-5/.
5. **Async support.** StrongInject fully supports async initialization and disposal, a feature sorely lacking in many IOC containers.

## Requirements

[Visual Studio 16.8](https://visualstudio.microsoft.com/vs) or greater

[.NET 5 sdk](https://dotnet.microsoft.com/download/dotnet/5.0) or greater

## Nuget

https://www.nuget.org/packages/StrongInject/

`<PackageReference Include="StrongInject" Version="1.0.0" />`

## How It Works

To use StrongInject, you first need to tell StrongInject the top-level services you would like to resolve. You do this by adding a new class implementing `IContainer<T>`. This will be your container. If you want to resolve multiple top-level services, then you can implement `IContainer<T>` multiple times on one container, or create multiple containers. StrongInject will then check at compile time that you've registered everything you need with the container to enable it to resolve all the top-level services. If you haven't, the compilation will fail with an error explaining what's gone wrong.

For example, if you want to resolve `MyApp` you might try doing this:

```csharp
using StrongInject;

public class MyService {}
public class MyApp { public MyApp(MyService myService) {} }

[Register(typeof(MyApp))]
public partial class MyContainer : IContainer<MyApp> {}
```

When you try compiling, it will fail with the following error: `SI0102: Error while resolving dependencies for 'MyApp': We have no source for instance of type 'MyService'`.

Now you fix it by adding a registration for `MyService`:

```csharp
using StrongInject;

public class MyService {}
public class MyApp { public MyApp(MyService myService) {} }

[Register(typeof(MyApp))]
[Register(typeof(MyService))]
public partial class MyContainer : IContainer<MyApp> {}
```

And this time when you compile, it will succeed and generate all the code needed to resolve an instance of `MyApp` at compile time.

What do I mean by a top-level service?

When using an IOC container, sometimes you request an instance from the container directly. These are top-level services. Most of the time the container resolves something though, you never ask for it explicitly - instead it's needed as a dependency for something else, which may itself be a dependency or a top-level service etc.

Ideally you want IOC containers to be non invasive - this means you write all your code as if there was no container, and then just use the container once to bootstrap your code. When writing code like this there should only ever be one top-level service. Sometimes this is not possible - for example when integrating with Asp.Net Core your controllers will usually need to be top-level services, but you should always try to minimize the number of top-level services where possible.

The next section will go into more detail about exactly how to register stuff with containers, and how to use them.

## Usage

The [wiki](https://github.com/YairHalberstadt/stronginject/wiki) is currently a work in progress. It aims to give a more thorough formal overview of everything in StrongInject, whereas this section of the readme gives a briefer overview relying heavily on examples. I would read through this first, then check out the wiki if you have any questions.

### Sample Projects

Check out these sample projects to help you get started:

##### [Asp.Net Core/Microsoft.Extensions.DependencyInjection](https://github.com/YairHalberstadt/stronginject/tree/master/Samples/AspNetCore)
##### [Console Application](https://github.com/YairHalberstadt/stronginject/tree/master/Samples/Console)
##### [Xamarin Application](https://github.com/YairHalberstadt/stronginject/tree/master/Samples/Xamarin)
##### [Wpf Application](https://github.com/YairHalberstadt/stronginject/tree/master/Samples/Wpf)

#### Real World Projects Using Stronginject

- [FluentLang](https://github.com/YairHalberstadt/fluentlang/blob/master/source/flc/DependencyInjection/FlcContainer.cs)

### Declaring a container
To create a container for a specific type, declare your a partial class and implement `StrongInject.IContainer<T>`:

```csharp
using StrongInject;

public class A {}

[Register(typeof(A))]
public partial class Container : IContainer<A> {}
```

If it's possible to resolve the type parameter StrongInject will generate the implementation of IContainer for you. Else it will produce an error diagnostic.

You can implement `IContainer<T>` for different values of `T`. They will all share SingleInstance dependencies.

```csharp
using StrongInject;

public class A {}
public class B {}

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>, IContainer<B> {}
```

### Using a container.

There are two ways to use a container - using the `Run` methods or the `Resolve` methods.

Either way you'll find it easier if you use the extension methods defined in `StrongInject.ContainerExtensions` rather than those defined directly on the container, so make sure you're `using StrongInject;`

The `Run` method on `IContainer<T>` takes a `Func<T, TResult>`. It resolves an instance of `T`, calls the func, disposes of any dependencies which require disposal, and then returns the result of the func. This ensures that you can't forget to dispose any dependencies, but you must make sure not too leak those objects out of the delegate. There are also overloads that allow you to pass in a void returning lambda.

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

In some cases this isn't flexible enough, for example if you want to use StrongInject from another IOC container, or you need more fine grained control over the lifetime of `T`.

For these cases you can call the `Resolve` method. This reurns an `Owned<T>` which is essentially a disposable wrapper over `T`. Make sure you call `Owned<T>.Dispose` once you're done using `Owned<T>.Value`.

```csharp
using StrongInject;

public class Program
{
  public static void Main()
  {
    using var ownedOfA = new Container().Resolve();
    var a = ownedOfA.Value;
    System.Console.WriteLine(a.ToString());
  }
}
```

### Registration

#### Basics

As you saw above, you can register a type with a container using the `RegisterAttribute`:

```csharp
using StrongInject;

public class A {}
public class B {}

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>, IContainer<B> {}
```

All the dependencies of the container type parameter must be registered or you will get a compile time error.

By default `[Register(typeof(A))]` will register a type `A` as itself. You can however register a type as any base type or implemented interface:

```csharp
using StrongInject;

public class BaseBase {}
public interface IBase {}
public class Base : BaseBase, IBase {}
public interface IA {}
public class A : Base, IA {}

[Register(typeof(A), typeof(IA), typeof(IBase), typeof(BaseBase))]
public partial class Container : IContainer<BaseBase> {}
```

If you do so, you will have to explicitly also register it as itself if that is desired: `[Register(typeof(A), typeof(A), typeof(IA), typeof(IBase), typeof(BaseBase))]`

If there is a single public non-parameterless constructor, StrongInject will use that to construct the type. If there is no public non-parameterless constructor StrongInject will use the parameterless constructor if it exists and is public. Else it will report an error.

#### Scope

The scope of a registration determines how often a new instance is created, how long it lives, and who uses it.

It can be set as the second parameter of a registration:

```csharp
using StrongInject;

public class A {}
public interface IB {}
public class B : IB {}

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B), Scope.InstancePerResolution, typeof(IB))]
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

You can add registrations to any type, and then import them using the `RegisterModuleAttribute`, or by inheriting from the module. This allows you to create reusable modules of common registrations.

```csharp
using StrongInject;

public class A {}

[Register(typeof(A))]
public class Module {}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A> {}
```

If you import multiple modules, and they both register the same type differently, you will get an error when trying to resolve an instance of the type.

There are two ways to solve this:

1. Register the type directly. This will override the registrations in imported modules.
2. Exclude the registration from one of the modules when you import it: `[RegisterModule(typeof(Module), exclusionList: new [] { typeof(A) })]`

#### Instance fields and properties

You can mark a field or a property with the `[Instance]` attribute, to register it as an instance of the field/property type. The field/property will be called for every dependency, but it is bad practice for it to be mutable or expensive, and so this should be irrelevant in practice.

```csharp
using StrongInject;
using System.Collections.Generic;

public class A
{
    public A(Dictionary<string, object> configuration){}
}

[Register(typeof(A))]
public partial class Container : IContainer<A>
{
    [Instance] Dictionary<string, object> _configuration;
    public Container(Dictionary<string, object> configuration) => _configuration = configuration;
}
```

If the instance field/property is defined on the container type, it can be private or public, instance or static. However if you want to export the instance as part of a module it must be public and static. If you inherit from the module, you can also access protected instances.

```csharp
using StrongInject;
using System;

public class A
{
    public A(IEqualityComparer<string> equalityComparer){}
}

public class StringEqualityComparerModule
{
    [Instance] public static IEqualityComparer<string> StringEqualityComparer = StringComparer.CurrentCultureIgnoreCase;
}

[Register(typeof(A))]
[RegisterModule(typeof(StringEqualityComparerModule))]
public partial class Container : IContainer<A>
{
}
```

If you want the instance to also be registered as its interfaces or base classes, or to be used as a factory for other types, you can configure all this and more using the `options` parameter. The above example could also have been registered like this:

```csharp
using StrongInject;
using System;

public class A
{
    public A(IEqualityComparer<string> equalityComparer){}
}

public class StringEqualityComparerModule
{
    [Instance(Options.AsImplementedInterfaces)] public static StringComparer StringEqualityComparer = StringComparer.CurrentCultureIgnoreCase;
}

[Register(typeof(A))]
[RegisterModule(typeof(StringEqualityComparerModule))]
public partial class Container : IContainer<A>
{
}
```

#### Factories

Sometimes a type requires more complex construction than just calling the constructor. For example you might want to hard code some parameters, or call a factory method. Some types don't have the correct constructors to be registered directly.

In such cases you can write a method returning the type you want to construct and mark it with the `[Factory]` attribute.

```csharp
using StrongInject;

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<IInterface[]>
{
    [Factory] private IInterface[] CreateInterfaceArray(A a, B b) => new IInterface[] { a, b };
}
```

You can set the scope of the factory method (how often it's called), by passing in the scope parameter: `[Factory(Scope.SingleInstance)]`.

If the factory method is defined on the container type, it can be private or public, instance or static. However if you want to export the factory method as part of a module it must be public and static. If you inherit from the module, you can also access protected factories.

```csharp
using StrongInject;

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}

public class Module
{
    [Factory] public static IInterface[] CreateInterfaceArray(A a, B b) => new IInterface[] { a, b };
}

[Register(typeof(A))]
[Register(typeof(B))]
[RegisterModule(typeof(Module))]
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

[Register(typeof(A))]
[Register(typeof(B))]
[RegisterFactory(typeof(InterfaceArrayFactory))]
public partial class Container : IContainer<IInterface[]> { }
```

Whilst a factory doesn't have to be a record, doing so significantly shortens the amount of code you have to write.

The scope of the factory and the factory target is controlled separately. This allows you to e.g. have a singleton factory, but call `CreateAsync` on every resolution:

```csharp
[RegisterFactory(typeof(InterfaceArrayFactory), scope: Scope.SingleInstance, factoryTargetScope: Scope.InstancePerResolution, typeof(IFactory<IInterface[]>))]
```

If a factory implements `IFactory<T>` for multiple `T`s it will be registered as a factory for all of them.

#### Generic Factory Methods

A factory method can be generic. All of the type parameters must be used in the return type. When resolving a type StrongInject will first look for non generic registrations which can resolve that type. If there are none, it will see if it can use any generic factory methods to resolve the type. For example this is how you could allow StrongInject to resolve an ImmutableArray:

```csharp
public class ImmutableArrayModule
{
    [Factory] public static ImmutableArray<T> CreateImmutableArray<T>(T[] arr) => arr.ToImmutableArray();
}
```

Generic methods can also have constraints. StrongInject will ignore generic methods during resolution if the constraints do not match.

#### Decorators

A decorator is a type which exposes a service by wrapping an underlying instance of the same service. Calls may pass straight through to the underlying service, or may be intercepted and custom behaviour applied. See https://en.wikipedia.org/wiki/Decorator_pattern.

You can register a type as a decorator using the `[RegisterDecorator(type, decoratedType)]` attribute.

Here is an example of how you could time how long a call took using the decorator pattern and stronginject.

```csharp
using System;
using System.Diagnostics;
using StrongInject

public class Foo {}
public interface IService
{
    Foo GetFoo()
}

public class Service : IService
{
    public Foo GetFoo() => new Foo();
}

public class ServiceTimingDecorator : IService
{
    private readonly IService _impl;
    public ServiceTimingDecorator(IService impl) => _impl = impl;
    public Foo GetFoo()
    {
        var watch = Stopwatch.StartNew();
        var foo = _impl.GetFoo();
        watch.Stop;
        Console.WriteLine($"Getting foo took {watch.ElapsedMilliseconds} ms");
        return foo;
    }
}

[Register(typeof(Service), typeof(IService))]
[RegisterDecorator(typeof(ServiceTimingDecorator), typeof(IService))]
public class Container : IContainer<IService> {}
```

StrongInject will resolve an instance of `Service`, and then wrap it in an instance of `ServiceTimingDecorator`. Whenever anyone asks for an `IService` they will get an instance of `ServiceTimingDecorator`.

You can't specify the scope of a decorator, as its scope is the same as that of the type it wraps.

The decorator constructor must have exactly one parameter of the decorated type, but can have any other parameters as well so long as they are not of the decorated type.

Instances provided by [delegate parameters](#delegate-support) are never decorated.

You can also define decorator factory methods, and even generic decorator factory methods via the `[DecoratorFactoryAttribute]`:

```csharp
[Register(typeof(Service), typeof(IService))]
public class Container : IContainer<IService>
{
    [DecoratorFactory] IService CreateDecorator(IService service) => new ServiceTimingDecorator(service);
}
```

Generic decorator factory methods gives you a powerful way to intercept resolution. For example you could theoretically use `Castle.DynamicProxy` along with a generic decorator factory to log every service thats created, or time all calls to all services:

```csharp
using Castle.DynamicProxy;
using StrongInject;
using System;
using System.Diagnostics;
using System.Threading;

public class Interceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var stopwatch = Stopwatch.StartNew();
        invocation.Proceed();
        stopwatch.Stop();
        Console.WriteLine($"Call to {invocation.Method.Name} took {stopwatch.ElapsedMilliseconds} ms");
    }
}

public class Foo { }
public class Bar { }

public interface IService1
{
    void Frob();
    Foo GetFoo();
}

public interface IService2
{
    Bar UseBar(Bar bar);
}

public class Service1 : IService1
{
    public void Frob()
    {
        Thread.Sleep(10);
    }

    public Foo GetFoo()
    {
        Thread.Sleep(20);
        return new Foo();
    }
}

public class Service2 : IService2
{
    public Bar UseBar(Bar bar)
    {
        Thread.Sleep(30);
        return bar;
    }
}

[Register(typeof(Service1), typeof(IService1))]
[Register(typeof(Service2), typeof(IService2))]
public partial class Container : IContainer<IService1>, IContainer<IService2>
{
    private readonly IInterceptor _interceptor = new Interceptor();
    private readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

    [DecoratorFactory]
    T Time<T>(T t) where T : class
    {
        if (typeof(T).IsInterface)
        {
            return _proxyGenerator.CreateInterfaceProxyWithTarget(t, _interceptor);
        }
        else
        {
            return _proxyGenerator.CreateClassProxyWithTarget(t, _interceptor);
        }
    }
}

public class Program
{
    public static void Main()
    {
        var container = new Container();
        container.Run<IService1>(x =>
        {
            x.Frob();
            _ = x.GetFoo();
        });
        container.Run<IService2>(x => _ = x.UseBar(new Bar()));
    }
}
```

The above program will print something like:

```
Call to Service1.Frob took 20 ms
Call to Service1.GetFoo took 26 ms
Call to Service2.UseBar took 42 ms
```

You can register multiple decorators for a type, and they will all be applied. As of now there is no way to control in which order they are applied, but the order is deterministic.

If you register an `[Instance]` and don't want it decorated, you can use `Options.DoNotDecorate`: `[Instance(Options.DoNotDecorate)]`.

Decorators are not disposed by default. For more information see [the wiki](https://github.com/YairHalberstadt/stronginject/wiki/Decorators#disposal).

#### Providing registrations at runtime or integrating with other IOC containers

What if you need to provide configuration for a registration at runtime? Or alternatively what if you need to integrate with an existing container?

We've already mentioned above that you can mark instance fields or properties on a container as `[Instance]`s. This allows you to access information only available at runtime during resolution:

```csharp
using StrongInject;

public class A
{
    public A(Configuration configuration){}
}

public class Configuration {}

[Register(typeof(A))]
public partial class Container : IContainer<A>
{
    [Instance] Configuration _configuration;
    public Container(Configuration configuration) => _configuration = configuration;
}
```

In some cases the runtime fields you need might be of a type that you would be hesitant to register, such as bool, or string. In such cases you can use factory methods to only access them where appropriate:

```csharp
using StrongInject;

public interface IInterface { }
public class A : IInterface { }
public class B : IInterface { }

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<IInterface>
{
    private readonly bool _useB;
    public Container(bool useB) => _useB = useB;

    [Factory]
    IInterface GetInterfaceInstance(Func<A> a, Func<B> b) => _useB ? b() : a();
}
```

In some cases though you need greater control over the disposal of the created instances. To do so you can make an `Instance` field implementing `IFactory<T>` or `IAsyncFactory<T>` and use `Options.UseAsFactory`. For example here's how you could integrate with Autofac:

```csharp
using StrongInject;

public class A
{
    public A(B b){} 
}
public class B {}

public class AutofacFactory<T>(Autofac.IContainer autofacContainer) : IFactory<T> where T : class
{
    private readonly ConcurrentDictionary<T, Autofac.Owned<T>> _ownedDic = new();
    private readonly Autofac.IContainer _autofacContainer;
    public AutofacFactory(Autofac.IContainer autofacContainer) => _autofacContainer = autofacContainer;
    public T Create()
    {
        var owned = _autofacContainer.Resolve<Owned<T>>();
        _ownedDic[owned.Value] = owned;
        return owned.Value;
    };
    public void Release(T instance)
    {
        if (_ownedDic.TryGetValue(instance, out var owned))
            owned.Dispose();
    }
}

[Register(typeof(A))]
public partial class Container : IContainer<A>
{
    [Instance(Options.AsImplementedInterfacesAndUseAsFactory)] private readonly AutofacFactory<B> _autofacFactory;
    public Container(AutofacFactory<B> autofacFactory) => _autofacFactory = autofacFactory;
}
```

This registers `AutofacFactory<B>` as all implemented interfaces, namely `IFactory<B>`. Since this is a factory, this also becomes a registration for `B` as well.

`Create` is called once per resolution (equiavalent to Instance Per Resolution scope). This can be adjusted further by registering it as `[Instance(Options.AsImplementedInterfacesAndUseAsFactory | Options.FactoryTargetScopeShouldBeSingleInstance)` for example.

#### How StrongInject picks which registration to use

It is possible for there to be multiple registrations for a type. In such a case resolving an instance of the type will result in an error, unless there is a best registration. The rule for picking the best registration is simple - any registration defined by a module is better than registrations defined on other modules that that module imports. 

So for example, imagine there is a `Container`, and two modules, `ModuleA` and `ModuleB`.

If all three define a registration for `SomeInterface`, then the one defined on the container will always be the best.

If just `ModuleA` and `ModuleB` define a registration for `SomeInterface` then things will depend:

- If `Container` imports both modules, resolving `SomeInterface` will always result in an error (even if `ModuleA` imports `ModuleB` or vice versa).
- If `Container` imports `ModuleA`, and `ModuleA` imports `ModuleB`, then `ModuleA`'s registration will be best.
- If `Container` imports `ModuleB`, and `ModuleB` imports `ModuleA`, then `ModuleB`'s registration will be best.

To fix errors as a result of multiple registrations for a type, the simplest thing to do is to add a single best registration directly to the container. If the container already has multiple registrations for the type, then move those registrations to a seperate module and import them.

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

[Register(typeof(A))]
[Register(typeof(B))]
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

[Register(typeof(Server))]
[Register(typeof(Handler))]
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

[Register(typeof(Server))]
[Register(typeof(Handler))]
public class Container : IContainer<Server> {}
```

### Post Constructor Initialization

If your type implements `IRequiresInitialization`, `Initialize` will be called after construction.
Whilst this is only useful in a few edge cases for synchronous methods, `IRequiresAsyncInitialization` is extremely useful as constructors cannot be async. Therefore I'll leave an example of using this API for the section on async support. 

### Async Support

Every interface used by StrongInject has an asynchronous counterpart.
There's `IAsyncContainer`, `IAsyncFactory` and `IRequiresAsyncInitialization`.

You can resolve an instance of `T` asynchronously from an `IAsyncContainer<T>` by calling `StrongInject.AsyncContainerExtensions.RunAsync`. RunAsync has overloads allowing you to pass in sync or async lambdas. As such `IAsyncContainer<T>` is useful even if resolution is completely synchronous if usage is asynchronous.

It is an error resolving `T` in an `IContainer<T>` if it depends on an asynchronous dependency.

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

[Register(typeof(PasswordChecker), Scope.SingleInstance)]
public partial class Container : IAsyncContainer<PasswordChecker>
{
    private readonly IDb _db;

    public Container(IDb db)
    {
        _db = db;
    }

    [Factory]
    IDb GetDb() => _db;
}

public static class Program
{
  public static async Task Main(string[] args)
  {
    await new Container(new Db()).RunAsync(x => 
      Console.WriteLine(x.CheckPassword(args[0], args[1])
        ? "Password is valid"
        : "Password is invalid"));
  }
}
```

#### Parallel Resolution

StrongInject makes the assumption that async dependencies will usually be doing IO. In order to increase performance it kicks off all async tasks in parallel as early as it can, and only awaits them once it's completed resolving all dependencies that don't require them. For example, if most of your components call the database as part of resolution, they will all do so in parallel rather than one after the other.

This improves async resolution time hugely - instead of resolution time being the sum of the time to resolve all components, it's the the amount of time it takes to resolve the longest chain of dependent components, no matter how many other components there are.

What this means is that components used as part of async resolution should be thread safe. If they are not, mark them as `InstancePerDependency`, which will ensure that they are only ever used by one thread of resolution at a time.

### Resolving all instances of a type

When resolving an array type, if there are no user provided registrations for the array type, the array will be created by resolving all registrations (including generic registrations) for the element type, and filling the array with these instances.

For example:

```csharp
public class A : IInterface {}
public class B : IInterface {}
public interface IInterface {}

[Register(typeof(A), typeof(IInterface))]
[Register(typeof(B), typeof(IInterface))]
public class Container : IContainer<IInterface[]>
```

This will resolve an array containing an instance of type `A` and an instance of type `B`.

The contents of the array are arbitrary but deterministic. A new array is created for every dependency, so users are free to mutate it.

Note that duplicate registrations will be deduplicated, so in the following case:

```csharp
public class A : IInterface {}
public interface IInterface {}

[Register(typeof(A), typeof(IInterface))]
[Register(typeof(A), typeof(IInterface))]
public class Container : IContainer<IInterface[]>
```

The array will contain 1 item, but in this case:

```csharp
public class A : IInterface {}
public interface IInterface {}

[Register(typeof(A), typeof(IInterface))]
[Register(typeof(A), Scope.SingleInstance, typeof(IInterface))]
public class Container : IContainer<IInterface[]>
```

It will contain 2 items.

### Optional Parameters

If a parameter to a type or method is optional, StrongInject will not error if it cannot be resolved, and will instead just use the default value.

An example of where this can be useful is for providing a default instance of an interface if none is registered:

```csharp
public class DefaultImplementation : IInterface {}
public interface IInterface {}

[Register(typeof(DefaultImplementation))]
public class Module
{
  [Decorator] GetIInterface(IInterface? impl = null, DefaultImplementation defaultImpl) => impl ?? defaultImpl;
}
```

### Disposal

Once a call to `Run` or `RunAsync` is complete, any Instance Per Resolution or Instance Per Dependency instances created as part of the call to `Run` or `RunAsync` will be disposed.

Similiarly when an `Owned<T>` is disposed, any Instance Per Resolution or Instance Per Dependency instances created as part of resolving `T` will be disposed.

In `RunAsync`/`ResolveAsync` if the types implement `IAsyncDisposable` it will be preferred over `IDisposable`. `Dispose` and `DisposeAsync` will not both be called, just `DisposeAsync`.
In `Run`/`Resolve`, `IAsyncDisposable` will be ignored. Only `Dispose` will ever be called.

Since `IFactory<T>` is free to create a new instance every time or return a singleton, StrongInject cannot call dispose directly. Instead it calls `IFactory<T>.Release(T instance)`. The factory is then free to dispose the class or not. When referencing the .NET Standard 2.1 package `Release` has a default implementation which does nothing. You only need to implement it if you want custom behaviour. If you reference the .NET Standard 2.0 package you will need to implement it either way.

Single Instance dependencies and their dependencies are disposed when the container is disposed. If the container implements `IAsyncDisposable` it must be disposed asynchronously even if it also implements `IDisposable`.

Note that dependencies may not be disposed in the following circumstances:
1. Resolution of a SingleInstance dependency throws.
2. Disposal of other dependencies throws.

When either of these happen it is most likely best to restart the application anyway, as a safe recovery is very unlikely.

### Thread Safety

StrongInject provides the following thread safety guarantees:
1. Resolution is thread safe, so long as it doesn't call back into the container recursively (e.g. a factory calling `container.RunAsync`).
2. If the container is disposed during resolution, then either dependencies will be created by the resolution, and will then be disposed, or resolution will throw and no dependencies will be created. Dependencies will not be created and then not disposed.
3. A SingleInstance dependency will never be created more than once.

### Inbuilt Modules

StrongInject provides a number of inbuilt modules which can be used 'out of the box' in the `StrongInject.Modules` namespace for the most commonly required functionality. You still need to import these modules via `RegisterModule`, although you can get all of the less opinionated modules in one go by importing the `StandardModule`.

At the moment the following modules are provided:

1. `LazyModule` (registers `Lazy<T>`)
1. `CollectionsModule` (registers `IEnumerable<T>`, `IReadOnlyList<T>` and `IReadOnlyCollection<T>`)
1. `ValueTupleModule` (registers all tuples from sizes 2 till 10)
1. `SafeImmutableArrayModule` (registers `ImmutableArray<T>`)
1. `UnsafeImmutableArrayModule` (provides a faster non-copying registration for `ImmutableArray<T>` which cannot be used if a custom registration for `T[]` exists)

At the moment the `StandardModule` imports `lazyModule`, `CollectionsModule` and `ValueTuple` module.

If you would like more modules/registrations added please open an issue.

## Product Roadmap

https://github.com/YairHalberstadt/stronginject/projects/1

## Contributing

This is currently in preview, meaning we can (and will) make API breaking changes. Now is the best time to file suggestions if you feel like the API could be approved.

Similiarly please do open issues if you spot any bugs.

Please feel free to work on any open issue and open a PR. Ideally open an issue before working on something, so that the effort doesn't go to waste if it's not suitable.

## Need Help?

I tend to hang around on gitter so feel free to chat at https://gitter.im/stronginject/community.

You can also [open an issue](https://github.com/YairHalberstadt/stronginject/issues/new/choose), ask on [stackoverflow](https://stackoverflow.com/questions/ask), or tag [me](https://twitter.com/HalberstadtYair) on twitter.
