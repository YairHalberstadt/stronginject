![](https://github.com/yairhalberstadt/stronginject/workflows/.NET%20Core/badge.svg)
# stronginject
compile time dependency injection for .Net

## Aims

1. **Compile time checked dependency injection.** If the type you're resolving isn't registered you get an exception at compile time, not runtime.
2. **Fast.** There's no dictionary lookups, no runtime code generation. Justest the fastest code it's possible to generate to resolve your type.
3. **Encourage best practices.** You can't use the container as a service locator. You can't forget to dispose the resolved types.
4. **No reflection or runtime code generation.** This uses roslyn Source Generators, meaning they're fast, and work well on UWP/IOS too.
5. **Async support.** StrongInject fully supports async initialization and disposal, a feature sorely lacking in many IOC containers.

## Usage

### Declaring a container
To create a container for a specific type, inherit from `StrongInject.IContainer<T>`:

```csharp
using StrongInject;

public class A {}

[Registration(typeof(A))]
public class Container : IContainer<A> {}
```

If it's possible to resolve the type parameter StrongInject will generate the implementation of IContainer for you. Else it will produce an error diagnostic.

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
