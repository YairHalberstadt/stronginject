<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Module Registration](#module-registration)
  - [Modules](#modules)
  - [Registering a module](#registering-a-module)
  - [Inheriting from a module](#inheriting-from-a-module)
  - [Example](#example)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Module Registration

Instead of having to repeat all your registrations for every single container, you can create reusable bags of registrations using a module, and then only register the module with your container to import all the module's registrations.

## Modules

To create a module, just add any [registrations](https://github.com/YairHalberstadt/stronginject/wiki/Registration) to a type, exactly like you would to a container.

The only difference is that [Instances](), [Factory Methods]() and [Decorator Factory Methods]() on a module must be either `public` and `static`, `protected`, or `protected internal`.

## Registering a module

To register a module us the `[RegisterModule]` attribute, defined as follows:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RegisterModuleAttribute : Attribute
{
    public RegisterModuleAttribute(Type type, params Type[] exclusionList);

    public Type Type { get; }
    public Type[] ExclusionList { get; }
}
```

The first parameter is the type of the module to register.

You can the optionally add a params list of types to exclude from the module.

When you register a module, you import all of the registrations from the module, except for any that provide instances of types specified in the `ExclusionList`, and any non `public static` members.

Modules can register other modules.

## Inheriting from a module

You can also inherit from a module. This is functionally the same as importing the module, except that you also import `protected` and `protected internal` members.

## Example

```csharp
using StrongInject;

public class A { }
public class B { public B(A a) { } }
public class C { public C(B b, int i) { } }

[Register(typeof(A))]
public class ModuleA { }

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B))]
public class ModuleB { }

public class ModuleC
{
    private int _i;
    public ModuleC(int i) => _i = i;
    protected C CreateC(B b) => new C(b, _i);
}

[RegisterModule(typeof(ModuleA))]
[RegisterModule(typeof(ModuleB), typeof(A))] // Have to exclude A, as otherwise we will have multiple conflicting registrations for A
public partial class Container : ModuleC, IContainer<C> // By inheriting from ModuleC we import the protected method CreateC
{
    public Container(int i) : base(i) { }
}
```
