# Containers

A container is esentially a factory that knows how to provide an instance of a type on demand, and then dispose of it once it's no longer needed.

You can create a container by inheriting from `IContainer<T>`, or `IAsyncContainer<T>`. The latter is necessary if resolution has to be asynchronous.

You need to make sure that the container is capable of [[resolving|Resolution]] `T` by providing suitable [[registrations|Registration]]. Otherwise you will get a compile time error.

There are two possible ways to use the container:

## Run/RunAsync

These methods take a delegate with 1 parameter of type `T`, resolvee an instance of `T` and then call the delegate. Once the delegate is complete, they dispose of any dependencies that were created as part of the resolution.

For example:

```csharp
using StrongInject;

[Register(typeof(A))]
public class MyContainer : IContainer<A>() {}

public class A : IDisposable { public void Dispose(){} }

var myContainer = new MyContainer();
myContainer.Run(a => Console.WriteLine($"We've resolved an instance of A: {a.ToString()}!!"));
```

You can also return a result from `Run` and use that:

```csharp
using StrongInject;

[Register(typeof(A))]
public class MyContainer : IContainer<A>() {}

public class A : IDisposable { public void Dispose(){} }

var myContainer = new MyContainer();
var aString = myContainer.Run(a => a.ToString());
Console.WriteLine($"We've resolved an instance of A: {aString()}!!");
```

Either way, you must make sure that the resolved type, and any of its dependencies, do not escape the scope of the delegate, or you may end up using them after they are disposed.

## Resolve/ResolveAsync

In some cases you need greater control over the lifetime of the resolved type, and when it is resolved. In such cases, you can call `Resolve`/`ResolveAsync`. These methods return an `Owned<T>`/`AsyncOwned<T>`. You can access the resolved instance via `Owned<T>.Value`/`AsyncOwned<T>.Value` and must manually dispose of `T` and all its dependencies by calling `Owned<T>.Dispose()`/`AsyncOwned<T>.DisposeAsync()`.
Note that it is not sufficient to dispose `T` directly, as that will not dispose of resolved dependencies.

## Disposal

Once you are finished using your container, make sure to dispose of it to dispose of any Single Instance dependencies.
