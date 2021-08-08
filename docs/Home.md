<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [StrongInject](#stronginject)
  - [Aims](#aims)
    - [Compile time checked dependency injection](#compile-time-checked-dependency-injection)
    - [Fast](#fast)
    - [Encourage best practices](#encourage-best-practices)
    - [No reflection or runtime code generation](#no-reflection-or-runtime-code-generation)
    - [Async support](#async-support)
  - [Concepts](#concepts)
    - [Containers](#containers)
    - [Registration](#registration)
    - [Resolution](#resolution)
    - [Disposal](#disposal)
  - [Getting Started](#getting-started)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# StrongInject

StrongInject is a compile time IOC framework for C#, utilizing the new roslyn [Source Generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) feature.

## Aims

### Compile time checked dependency injection

If the type you're resolving isn't registered you get an error at compile time, not runtime.

### Fast

There's no dictionary lookups, no runtime code generation. Just the fastest code it's possible to generate to resolve your type.

### Encourage best practices

You can't use the container as a service locator. You can't forget to dispose the resolved types.

### No reflection or runtime code generation

Instead StrongInject uses roslyn Source Generators, meaning it's fast, and works well on UWP/IOS too. This also means it's linker friendly - see https://devblogs.microsoft.com/dotnet/app-trimming-in-net-5/.

### Async support

StrongInject fully supports async initialization and disposal, a feature sorely lacking in many IOC containers.

## Concepts

### [Containers](https://github.com/YairHalberstadt/stronginject/wiki/Containers)

A container is esentially a factory that knows how to provide an instance of a type on demand, and then dispose of it once it's no longer needed.

### [Registration](https://github.com/YairHalberstadt/stronginject/wiki/Registration)

Registration is how you let your container know what it can use, and how, to try and create that instance.

### [Resolution](https://github.com/YairHalberstadt/stronginject/wiki/Resolution)

Resolution is how the container create/provides an instance of a type. This can be when you ask for the instance directly, or it may be needed as a dependency for another resolution.

### Disposal

Once an instance is no longer needed, StrongInject takes care of disposing it for you.

## Getting Started

1. Install the package from [NuGet](https://www.nuget.org/packages/StrongInject/)
2. Create a container for the type you want to resolve, and register any dependencies:
    ```csharp
    using StrongInject;
    
    [Register(typeof(A))]
    [Register(typeof(B))]
    public class MyContainer : IContainer<A>() {}
    
    public class A { public A(B b){} }
    public class B {}
    ```
    To find out more about registration see the [documentation](https://github.com/YairHalberstadt/stronginject/wiki/Registration).
3. Use the container:
    ```csharp
    var myContainer = new MyContainer();
    myContainer.Run(a => Console.WriteLine($"We've resolved an instance of A: {a.ToString()}!!"));
    ```
    To find out more about resolution see the [documentation](https://github.com/YairHalberstadt/stronginject/wiki/Resolution).
