<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Resolution](#resolution)
  - [Resolution Algorithm](#resolution-algorithm)
    - [Order Providers Are Checked](#order-providers-are-checked)
      - [Delegate Parameters](#delegate-parameters)
      - [Non Generic Registrations](#non-generic-registrations)
      - [Generic Registrations](#generic-registrations)
      - [Delegate Types](#delegate-types)
      - [Array Types](#array-types)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Resolution

Resolution is how the container create/provides an instance of a type. This can be when you ask for the instance directly, or it may be needed as a dependency for another resolution.

To find out more about using containers see the [documentation](https://github.com/YairHalberstadt/stronginject/wiki/Containers).

## Resolution Algorithm

Resolution is recursive. When you request an instance of a type, StrongInject will find the best way to resolve it. If doing so requires some dependencies, StrongInject will resolve them too, and so on.

For example:

```csharp
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
public class MyContainer : IContainer<A>() {}

public class A { public A(B b){} }
public class B { public B(C c){} }
public class C {}
```

1. StrongInject will try to resolve `A`, and finds that we've registered the type `A`, so will try to use `A`'s constructor to create an instance of `A`.
2. `A`'s constructor requires an instance of `B` as a parameter.
3. StrongInject will try to resolve `B`, and finds that we've registered the type `B`, so will try to use `B`'s constructor to create an instance of `B`.
4. `B`'s constructor requires an instance of `B` as a parameter.
5. StrongInject will try to resolve `C`, and finds that we've registered the type `C`, so will try to use `C`'s constructor to create an instance of `C`.
6. `C`'s constructor has no parameters, and so we are done.

### Order Providers Are Checked

When StrongInject needs to resolve an instance of a type, it will check the following potential providers one by one, and stop once it finds any that are capable of providing the instance.

#### Delegate Parameters

StrongInject can automatically resolve delegates. When resolving the return type of the delegate, StrongInject will make use of any of the delegate parameters for use as dependencies of the return type.
For a delegate resolved inside a delegate, inner delegate parameters override outer delegate parameters of the same type.
Each delegate parameter can only be used to resolve an instance of the exact same type as the parameter. It cannot be used to resolve as the parameters base classes, or interface implementations.

#### Non Generic Registrations

To find out more about using registration see the [documentation](https://github.com/YairHalberstadt/stronginject/wiki/Registration). Most registrations are non generic, and StrongInject checks to see if there are any such registrations for the type. If there is a best registration, this is used. If there are multiple best registrations, then StrongInject produces an error message and stops.

#### Generic Registrations

To find out more about using registration see the [documentation](https://github.com/YairHalberstadt/stronginject/wiki/Registration). Some registrations can be generic, and if there are no non generic registrations for the type, StrongInject checks to see if any generic registrations can be used to create the type by substituting in the correct type parameters. If there is a best such registration, this is used. If there are multiple best registrations, then StrongInject produces an error message and stops.

#### Delegate Types

If the type is a delegate type, StrongInject will automatically create a delegate which resolves an instance of the delegate return type, using the delegate parameters as dependencies if necessary.
If the return type is a `TaskT>` or `ValueTask<T>`, then StrongInject will create an async delegate which asynchronously resolves an instance of `T`, using the delegate parameters as dependencies if necessary.

#### Array Types

If the type is an array, StrongInject will find all non generic and generic registrations which can be used to resolve the element type of the array. It will then resolve all of them and create an array containing all of these instances.
