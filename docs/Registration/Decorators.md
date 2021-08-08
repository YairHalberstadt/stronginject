<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Decorators](#decorators)
  - [Decorator Registration](#decorator-registration)
    - [Decorator Type Registration](#decorator-type-registration)
    - [Decorator Factory Method Registration](#decorator-factory-method-registration)
  - [Disposal](#disposal)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Decorators

A Decorator is different to other registrations, in that it does not provide an instance of a type, but rather wraps/modifies an *underlying* instance of a type, which is redolved in the normal manner.

If multiple decorators are registered for a type, all of them will be applied, by wrapping one decorator in another, onion style. The order in which decorators wrap each other is deterministic but an implmentation detail - you should not rely on this order.

Decorators are not applied to delegate parameters, or to `[Instance]` fields and properties with Options.DoNotDecorate applied. They are applied to everything else.

## Decorator Registration

There are two ways to register decorators.

### [Decorator Type Registration](https://github.com/YairHalberstadt/stronginject/wiki/DecoratorTypeRegistration)

You can register a type as a decorator for an interface it implements, if its constructor has exactly one parameter whose type is the interface.

### Decorator Factory Method Registration

You can register a method returning `T` as a decorator of `T` if it has exactly one parameter of type `T`.

## Disposal

Decorators are not disposed by default, for a number of reasons:
1. In many cases a decorator implements `IDisposable` as the interface requires it, but does not actually require disposal.
2. In many cases a decorator will delegate to the underlying's `Dispose` method:
   1. Since the underlying is disposed separately, this can lead to double disposal.
   2. The underlying may be an `Instance` field or property, which should never be disposed.
3. In many cases a `DecoratorFactory` may return the same instance as was passed in, also leading to issues 2.i and 2.ii.

If your decorator needs to be disposed, make sure it does not dispose the underlying instance, only resources it owns directly. Use `DecoratorOptions.Dispose` to mark it as requiring disposal.

Decorators are disposed from outermost to innermost, followed by the underlying instance.
