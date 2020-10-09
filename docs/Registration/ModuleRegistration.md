# Module Registration

Instead of having to repeat all your registrations for every single container, you can create reusable bags of registrations using a module, and then only register the module with your container to import all the module's registrations.

## Modules

To create a module, just add any [registrations](https://github.com/YairHalberstadt/stronginject/wiki/Registration) to a type, exactly like you would to a container.

The only difference is that [Instances](), [Factory Methods]() and [Decorator Factory Methods]() on a module must be `public` and `static`.

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

When you register a module, you import all of the registrations from the module, except for any that provide instances of types specified in the `ExclusionList`.

Modules can register other modules.

## Example

```csharp
public class A {}
public class B { public B(A a){} }
public class C { public C(B b){} }

[Register(typeof(A))]
public class ModuleA {}

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B))]
public class ModuleB {}

[Register(typeof(C))]
[RegisterModule(typeof(ModuleA))]
[RegisterModule(typeof(ModuleB), typeof(A))] // Have to exclude A, as otherwise we will have multiple conflicting registrations for A
public class Container : IContainer<C>{}
```
