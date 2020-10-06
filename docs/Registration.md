# Registration

A container is esentially a factory that knows how to create an instance of a type on demand, and then dispose of it once it's no longer needed.

Registration is how you let your container know what it can use, and how, to try and create that instance.

## Forms of Registration

StrongInject currently supports the following forms of Registration:

### Module Registration

Instead of having to repeat all your registrations for every single container, you can create reusable bags of registrations using a module, and then only register the module with your container to import all the module's registrations.

### Type Registration

You can register a type either as itself, or as its base classes/interfaces. StrongInject will look for a suitable constructor to use to instantiate it.

### Instance Registration

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
