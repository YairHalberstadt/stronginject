<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Registration](#registration)
  - [Forms of Registration](#forms-of-registration)
    - [Module Registration](#module-registration)
    - [Type Registration](#type-registration)
    - [Instance Registration](#instance-registration)
    - [Factory Type Registration](#factory-type-registration)
    - [Factory Method Registration](#factory-method-registration)
    - [Decorators](#decorators)
      - [Decorator Type Registration](#decorator-type-registration)
      - [Decorator Factory Method Registration](#decorator-factory-method-registration)
  - [Other registration concepts](#other-registration-concepts)
    - [Scopes](#scopes)
    - [Options](#options)
    - [Best Registration](#best-registration)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Registration

A container is esentially a factory that knows how to provide an instance of a type on demand, and then dispose of it once it's no longer needed.

Registration is how you let your container know what it can use, and how, to try and create that instance.

## Forms of Registration

StrongInject currently supports the following forms of Registration:

### [Module Registration](https://github.com/YairHalberstadt/stronginject/wiki/ModuleRegistration)

Instead of having to repeat all your registrations for every single container, you can create reusable bags of registrations using a module, and then only register the module with your container to import all the module's registrations.

### [Type Registration](https://github.com/YairHalberstadt/stronginject/wiki/TypeRegistration)

You can register a type either as itself, or as its base classes/interfaces. StrongInject will look for a suitable constructor to use to instantiate it.

### [Instance Registration](https://github.com/YairHalberstadt/stronginject/wiki/InstanceRegistration)

You can register a field or property as storing an instance of a type.

### Factory Type Registration

If a type implements `IFactory<T>` or `IAsyncFactory<T>` you can register it as a factory of `T`.

### Factory Method Registration

You can register a method returning `T` as a factory for `T`.

### Decorators

Decorators are used to modify an instance created by another registration. There are two forms of registering decorators:

#### Decorator Type Registration

You can register a type as a decorator for an interface it implements, if its constructor has exactly one parameter whose type is the interface.

#### Decorator Factory Method Registration

You can register a method returning `T` as a decoraor of `T` if it has exactly one parameter of type `T`.

## Other registration concepts

### Scopes

All registrations have a particular `Scope`. This tells StrongInject how long an instance resolved using that registration should live for, and how widely it should be shared.

### Options

Some registrations can have their behavior modified using the `Options` enum. This allows for all sorts of customization of the registration.

### Best Registration

If there are multiple registrations for a type, StrongInject will have to pick the best registration to use, or error if there is none.
