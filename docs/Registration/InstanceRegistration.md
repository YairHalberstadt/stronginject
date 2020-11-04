<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Instance Registration](#instance-registration)
  - [Usage in modules](#usage-in-modules)
  - [Configuration](#configuration)
    - [Decoration](#decoration)
    - [Disposal](#disposal)
    - [Decorators](#decorators)
  - [Example](#example)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Instance Registration

You can mark a field or property with the `[Instance]` attribute:

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class InstanceAttribute : Attribute
{
    public InstanceAttribute(Options options = Options.Default);

    public Options Options { get; }
}
```

Any field/property so marked will be registered as an instance for the type of the field/property.

How often the field will be accessed is an implementation detail, so the field/property must not change whilst the container is alive.

## Usage in modules

In order for an instance field or property to be exported by a module it must be `public` and `static`. For it to be inherited it can also be `protected`. Instance fields and properties in containers can be `private` and `instance` if desired.

## Configuration

The registration can be modified using the `options` parameter. See the [documentation](https://github.com/YairHalberstadt/stronginject/wiki/Registration#options) for more details.

This allows you to register the instance as more than just the type of the the field/property. In particular it is possible to register the instance as it's:

1. Base classes
2. Interfaces
3. If it's an `IFactory<T>` or an `IAsyncFactory<T>`, as `T`, as well as to register `T`'s base classes, interfaces, and factories etc.

### Decoration

By default all instances can be decorated. This can be opted out of by applying `Options.DoNotDecorate`.

### Disposal

Instance fields and properties will not be disposed by StrongInject. This is necessary so that modules containing instance fields and properties can be shared among multiple containers.

### Decorators

By default Instance fields and properties will be decorated. If you do not want this to happen, apply the `Options.DoNotDecorate` parameter.

## Example

```csharp
using StrongInject;
using System;

public class A { }
public class B : IDisposable { public void Dispose() { } } // Dispose will never be called
public class C { public C(A a, B b) { } }

public class Module
{
    [Instance(Options.DoNotDecorate)] public static readonly B _b = new B(); // Must be public and static.
}

[Register(typeof(C))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<C>
{
    [Instance] private readonly A _a; // Can be private and instance.
    public Container(A a) => _a = a;
}
```
